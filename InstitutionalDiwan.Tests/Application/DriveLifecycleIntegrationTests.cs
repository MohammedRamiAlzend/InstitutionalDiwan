using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>
/// Module 11 Phase 5 end-to-end integration: one continuous scenario over the real SQLite
/// repositories, the real access evaluator and the in-memory S3 stand-in — space → folders →
/// upload → inherited then broken permissions → OnlyOffice edit/save → versions → restore →
/// deletion request → head approval → trash → restore → permanent purge → quota accounting.
/// </summary>
public sealed class DriveLifecycleIntegrationTests
{
    private const long DefaultQuota = 5L * 1024 * 1024 * 1024;
    private const string WordMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    [Fact]
    public async Task FullLifecycle_FromSpaceCreationToPermanentPurge()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var owner = world.World.Owner;
        var member = world.World.Member;
        var head = world.World.Head;
        var admin = world.World.Admin;
        var ct = CancellationToken.None;

        // 1) Space — a fresh shared space owned by the caller, default 5 GB quota.
        var space = await world.CreateSpace(owner).Handle(new CreateDriveSpaceCommand("مساحة التكامل"), ct);
        Assert.True(space.IsSuccess);
        var spaceId = space.Value.SpaceId;
        Assert.Equal(owner.UserId, space.Value.OwnerUserId);
        Assert.Equal(DefaultQuota, space.Value.EffectiveQuotaBytes);
        Assert.Equal(0, space.Value.UsedBytes);

        // 2) Folders — root → child.
        var root = await world.CreateFolder(owner).Handle(new CreateDriveFolderCommand(spaceId, null, "الجذر"), ct);
        Assert.True(root.IsSuccess);
        var child = await world.CreateFolder(owner).Handle(new CreateDriveFolderCommand(spaceId, root.Value.ItemId, "فرعي"), ct);
        Assert.True(child.IsSuccess);
        Assert.Equal(root.Value.ItemId, child.Value.ParentId);

        // 3) Upload — a .docx into the child folder; initiate reserves v1, complete commits it.
        var v1Bytes = Enumerable.Repeat((byte)0x11, 1024).ToArray();
        var initiated = await world.InitiateUpload(owner).Handle(
            new InitiateDriveUploadCommand(spaceId, child.Value.ItemId, "تقرير.docx", v1Bytes.LongLength, WordMime), ct);
        Assert.True(initiated.IsSuccess);
        Assert.Equal(1, initiated.Value.VersionNumber);
        var fileId = initiated.Value.ItemId;
        var v1Id = initiated.Value.VersionId;
        world.Storage.PutObject(initiated.Value.ObjectKey, v1Bytes);

        var completed = await world.CompleteUpload(owner).Handle(new CompleteDriveUploadCommand(v1Id), ct);
        Assert.True(completed.IsSuccess);
        Assert.True(completed.Value.IsCurrent);
        Assert.Equal(1024, await UsedBytesAsync(world, spaceId));

        // 4) Permissions — Editor on the root inherits down to the file…
        var grant = await world.Grant(owner).Handle(
            new GrantDrivePermissionCommand(root.Value.ItemId, DrivePrincipalType.UserId, member.UserId, DriveRole.EditorId), ct);
        Assert.True(grant.IsSuccess);
        Assert.Equal(DriveRole.EditorId, await world.AccessFor(member).GetEffectiveRoleAsync(fileId));

        // …breaking inheritance on the file severs the ancestor grant…
        var broken = await world.Inheritance(owner).Handle(new SetDriveItemInheritanceCommand(fileId, true), ct);
        Assert.True(broken.IsSuccess);
        Assert.Equal(0, await world.AccessFor(member).GetEffectiveRoleAsync(fileId));

        // …and a direct Viewer grant on the file restores a lesser role.
        var direct = await world.Grant(owner).Handle(
            new GrantDrivePermissionCommand(fileId, DrivePrincipalType.UserId, member.UserId, DriveRole.ViewerId), ct);
        Assert.True(direct.IsSuccess);
        Assert.Equal(DriveRole.ViewerId, await world.AccessFor(member).GetEffectiveRoleAsync(fileId));

        // 5) Editing — the owner opens an edit session, OnlyOffice saves a new version via callback.
        var config = await world.EditConfig(owner).Handle(new GetDriveEditConfigQuery(fileId), ct);
        Assert.True(config.IsSuccess);
        Assert.Equal("edit", config.Value.Mode);
        Assert.Equal("word", config.Value.DocumentType);
        var document = (IReadOnlyDictionary<string, object?>)config.Value.Config["document"]!;
        var key = Assert.IsType<string>(document["key"]);
        Assert.NotNull(world.Tokens.Verify(Assert.IsType<string>(config.Value.Config["token"])));

        var edited = Enumerable.Repeat((byte)0x22, 2048).ToArray();
        world.Downloader.Bytes = edited;
        var save = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(CallbackPayload(world, key, 2, "https://onlyoffice.local/tmp/save")), ct);
        Assert.Equal(0, save.Value.Error);

        var afterSave = await world.World.Versions.ListByItemAsync(fileId);
        Assert.Equal(2, afterSave.Count);
        var v2 = afterSave.Single(v => v.VersionNumber == 2);
        Assert.Equal(DriveVersionSource.EditorSaveId, v2.SourceId);
        Assert.True(v2.IsCommitted);
        Assert.Equal(owner.UserId, v2.CreatedByUserId);
        Assert.Equal(1024 + 2048, await UsedBytesAsync(world, spaceId));
        var fileAfterSave = await world.World.Items.GetByIdAsync(fileId);
        Assert.Equal(v2.VersionId, fileAfterSave!.CurrentVersionId);

        // 6) Versions — restoring v1 produces a new copy (v3, Restore source) and bumps usage again.
        var restoredVersion = await world.RestoreVersion(owner).Handle(new RestoreDriveFileVersionCommand(fileId, v1Id), ct);
        Assert.True(restoredVersion.IsSuccess);
        Assert.Equal(3, restoredVersion.Value.VersionNumber);
        Assert.Equal(DriveVersionSource.RestoreId, restoredVersion.Value.SourceId);
        Assert.True(restoredVersion.Value.IsCurrent);
        Assert.Equal(3, (await world.World.Versions.ListByItemAsync(fileId)).Count);
        Assert.Equal(1024 + 2048 + 1024, await UsedBytesAsync(world, spaceId));

        // 7) Deletion request — the owner asks; the department head is notified.
        var request = await world.RequestDeletion(owner).Handle(new RequestDriveItemDeletionCommand(fileId, "لم يعد مطلوبًا"), ct);
        Assert.True(request.IsSuccess);
        Assert.Equal(DriveDeletionRequestStatus.PendingId, request.Value.StatusId);
        Assert.Contains(world.Notifications.Published, n =>
            n.TypeId == NotificationType.DriveDeletionRequestedId && n.Recipients.Contains(head.UserId));

        // 8) Approval — the head approves; the file lands in the trash and leaves the live browse.
        var approved = await world.ApproveDeletion(head).Handle(new ApproveDriveDeletionCommand(request.Value.RequestId, "موافق"), ct);
        Assert.True(approved.IsSuccess);
        Assert.Equal(DriveDeletionRequestStatus.ApprovedId, approved.Value.StatusId);
        Assert.True((await world.World.Items.GetByIdAsync(fileId))!.IsTrashed);

        var liveBrowse = await world.Browse(owner).Handle(new BrowseDriveItemsQuery(SpaceId: spaceId), ct);
        Assert.DoesNotContain(liveBrowse.Value.Items, i => i.ItemId == fileId);
        var trashBrowse = await world.Browse(owner).Handle(new BrowseDriveItemsQuery(SpaceId: spaceId, Trashed: true), ct);
        Assert.Contains(trashBrowse.Value.Items, i => i.ItemId == fileId);

        // 9) Restore — the owner pulls it back out of the trash.
        var restoredItem = await world.RestoreItem(owner).Handle(new RestoreDriveItemCommand(fileId), ct);
        Assert.True(restoredItem.IsSuccess);
        Assert.Null(restoredItem.Value.TrashedAt);
        Assert.False((await world.World.Items.GetByIdAsync(fileId))!.IsTrashed);

        // 10) Permanent — request again, head approves, admin purges: rows and S3 objects gone.
        var secondRequest = await world.RequestDeletion(owner).Handle(new RequestDriveItemDeletionCommand(fileId, "حذف نهائي"), ct);
        Assert.True(secondRequest.IsSuccess);
        var secondApproval = await world.ApproveDeletion(head).Handle(new ApproveDriveDeletionCommand(secondRequest.Value.RequestId), ct);
        Assert.True(secondApproval.IsSuccess);

        var purged = await world.PurgeItem(admin, isAdmin: true).Handle(new PurgeDriveItemCommand(fileId), ct);
        Assert.True(purged.IsSuccess);
        Assert.Null(await world.World.Items.GetByIdAsync(fileId));
        Assert.False(world.Storage.Exists(initiated.Value.ObjectKey));
        Assert.False(world.Storage.Exists(v2.ObjectKey));
        Assert.Empty(await world.World.Versions.ListByItemAsync(fileId));

        // 11) Quota — every committed version's bytes were released on purge.
        Assert.Equal(0, await UsedBytesAsync(world, spaceId));
    }

    private static DriveOnlyOfficeCallbackPayload CallbackPayload(DriveHandlerWorld world, string key, int status, string? url)
    {
        var token = world.Tokens.Sign(new Dictionary<string, object?> { ["key"] = key, ["status"] = status, ["url"] = url });
        return new DriveOnlyOfficeCallbackPayload(key, status, url, null, null, token);
    }

    private static async Task<long> UsedBytesAsync(DriveHandlerWorld world, Guid spaceId)
        => (await world.World.Spaces.GetByIdAsync(spaceId))!.UsedBytes;
}