using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DriveDeletionHandlerTests
{
    private static async Task<DriveItem> AddPersonalFileAsync(DriveHandlerWorld world, string name)
    {
        var file = DriveItem.CreateFile(world.World.Personal.SpaceId, null, name, world.World.Owner.UserId).Value;
        world.World.Db.DriveItems.Add(file);
        await world.World.Db.SaveChangesAsync();
        return file;
    }

    [Fact]
    public async Task Request_NotifiesDepartmentHead()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = await AddPersonalFileAsync(world, "شخصي.txt");

        var result = await world.RequestDeletion(world.World.Owner).Handle(
            new RequestDriveItemDeletionCommand(file.ItemId, "لم يعد مطلوبًا"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DriveDeletionRequestStatus.PendingId, result.Value.StatusId);
        var notification = world.Notifications.Published.Single(n => n.TypeId == NotificationType.DriveDeletionRequestedId);
        Assert.Contains(world.World.Head.UserId, notification.Recipients);
    }

    [Fact]
    public async Task Request_AdminFallback_WhenNoHead()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = DriveItem.CreateFile(world.World.Foreign.SpaceId, null, "غريب.txt", world.World.Stranger.UserId).Value;
        world.World.Db.DriveItems.Add(file);
        await world.World.Db.SaveChangesAsync();

        var result = await world.RequestDeletion(world.World.Stranger).Handle(
            new RequestDriveItemDeletionCommand(file.ItemId, "سبب"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var notification = world.Notifications.Published.Single(n => n.TypeId == NotificationType.DriveDeletionRequestedId);
        Assert.Contains(world.World.Admin.UserId, notification.Recipients);
        Assert.DoesNotContain(world.World.Head.UserId, notification.Recipients);
    }

    [Fact]
    public async Task Request_SecondPending_DeletionPending()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = await AddPersonalFileAsync(world, "شخصي.txt");
        await world.RequestDeletion(world.World.Owner).Handle(new RequestDriveItemDeletionCommand(file.ItemId, "أول"), CancellationToken.None);

        var second = await world.RequestDeletion(world.World.Owner).Handle(new RequestDriveItemDeletionCommand(file.ItemId, "ثانٍ"), CancellationToken.None);
        Assert.Equal("Drive.DeletionPending", second.Error.Code);
    }

    [Fact]
    public async Task Approve_ByHead_TrashesItem_AndNotifiesRequester()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var request = await world.RequestDeletion(world.World.Owner).Handle(
            new RequestDriveItemDeletionCommand(world.World.DeepFile.ItemId, "سبب"), CancellationToken.None);

        var approved = await world.ApproveDeletion(world.World.Head).Handle(
            new ApproveDriveDeletionCommand(request.Value.RequestId, "موافق"), CancellationToken.None);

        Assert.True(approved.IsSuccess);
        Assert.Equal(DriveDeletionRequestStatus.ApprovedId, approved.Value.StatusId);
        var item = await world.World.Items.GetByIdAsync(world.World.DeepFile.ItemId);
        Assert.True(item!.IsTrashed);
        Assert.Contains(world.Notifications.Published, n =>
            n.TypeId == NotificationType.DriveDeletionApprovedId && n.Recipients.Contains(world.World.Owner.UserId));
    }

    [Fact]
    public async Task Approve_ByStranger_Denied()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var request = await world.RequestDeletion(world.World.Owner).Handle(
            new RequestDriveItemDeletionCommand(world.World.DeepFile.ItemId, "سبب"), CancellationToken.None);

        var denied = await world.ApproveDeletion(world.World.Stranger).Handle(
            new ApproveDriveDeletionCommand(request.Value.RequestId), CancellationToken.None);
        Assert.Equal("Drive.AccessDenied", denied.Error.Code);
    }

    [Fact]
    public async Task Reject_NotifiesRequester_AndKeepsItem()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var request = await world.RequestDeletion(world.World.Owner).Handle(
            new RequestDriveItemDeletionCommand(world.World.DeepFile.ItemId, "سبب"), CancellationToken.None);

        var rejected = await world.RejectDeletion(world.World.Head).Handle(
            new RejectDriveDeletionCommand(request.Value.RequestId, "مرفوض"), CancellationToken.None);
        Assert.True(rejected.IsSuccess);
        Assert.Equal(DriveDeletionRequestStatus.RejectedId, rejected.Value.StatusId);

        var item = await world.World.Items.GetByIdAsync(world.World.DeepFile.ItemId);
        Assert.False(item!.IsTrashed);
        Assert.Contains(world.Notifications.Published, n =>
            n.TypeId == NotificationType.DriveDeletionRejectedId && n.Recipients.Contains(world.World.Owner.UserId));
    }

    [Fact]
    public async Task Cancel_OnlyRequester()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var request = await world.RequestDeletion(world.World.Owner).Handle(
            new RequestDriveItemDeletionCommand(world.World.DeepFile.ItemId, "سبب"), CancellationToken.None);

        var denied = await world.CancelDeletion(world.World.Head).Handle(
            new CancelDriveDeletionCommand(request.Value.RequestId), CancellationToken.None);
        Assert.Equal("Drive.OnlyRequesterCanCancel", denied.Error.Code);

        var ok = await world.CancelDeletion(world.World.Owner).Handle(
            new CancelDriveDeletionCommand(request.Value.RequestId), CancellationToken.None);
        Assert.True(ok.IsSuccess);
        Assert.Equal(DriveDeletionRequestStatus.CancelledId, ok.Value.StatusId);
    }

    [Fact]
    public async Task DirectDelete_ByAdmin_Succeeds()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var result = await world.DeleteDirectly(world.World.Admin, isAdmin: true).Handle(
            new DeleteDriveItemDirectlyCommand(world.World.DeepFile.ItemId), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.TrashedAt);
    }

    [Fact]
    public async Task DirectDelete_ByOwnerNonHead_Denied_EvenPersonal()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = await AddPersonalFileAsync(world, "شخصي.txt");
        var result = await world.DeleteDirectly(world.World.Owner).Handle(
            new DeleteDriveItemDirectlyCommand(file.ItemId), CancellationToken.None);
        Assert.Equal("Drive.AccessDenied", result.Error.Code);
    }

    [Fact]
    public async Task DirectDelete_ByHeadWithManage_Succeeds()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        await world.World.AddGrantAsync(world.World.DeepFile.ItemId, DrivePrincipalType.UserId, world.World.Head.UserId, DriveRole.ManagerId);

        var result = await world.DeleteDirectly(world.World.Head).Handle(
            new DeleteDriveItemDirectlyCommand(world.World.DeepFile.ItemId), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.TrashedAt);
    }

    [Fact]
    public async Task Restore_ToRoot_WhenParentTrashed()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        world.World.ChildFolder.Trash(world.World.Owner.UserId);
        world.World.DeepFile.Trash(world.World.Owner.UserId);
        await world.World.Db.SaveChangesAsync();

        var restored = await world.RestoreItem(world.World.Owner).Handle(
            new RestoreDriveItemCommand(world.World.DeepFile.ItemId), CancellationToken.None);

        Assert.True(restored.IsSuccess);
        Assert.Null(restored.Value.ParentId);
        Assert.Null(restored.Value.TrashedAt);
    }

    [Fact]
    public async Task Restore_NameTaken_AtDestination()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        world.World.MemberFile.Trash(world.World.Owner.UserId);
        await world.World.Db.SaveChangesAsync();
        await world.CreateFolder(world.World.Owner).Handle(
            new CreateDriveFolderCommand(world.World.Shared.SpaceId, null, "ملاحظة.txt"), CancellationToken.None);

        var result = await world.RestoreItem(world.World.Owner).Handle(
            new RestoreDriveItemCommand(world.World.MemberFile.ItemId), CancellationToken.None);
        Assert.Equal("Drive.NameTaken", result.Error.Code);
    }

    [Fact]
    public async Task Purge_ByAdmin_RemovesObject_AndReleasesQuota()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var version = await world.World.AddVersionAsync(file, 1, commit: true);
        file.SetCurrentVersion(version.VersionId, 1, 1024, new string('a', 64));
        file.Trash(world.World.Owner.UserId);
        var space = await world.World.Spaces.GetByIdAsync(file.SpaceId);
        space!.AddUsage(1024, 5L * 1024 * 1024 * 1024);
        await world.World.Db.SaveChangesAsync();
        world.Storage.PutObject(version.ObjectKey, new byte[1024]);

        var result = await world.PurgeItem(world.World.Admin, isAdmin: true).Handle(
            new PurgeDriveItemCommand(file.ItemId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(world.Storage.Exists(version.ObjectKey));
        Assert.Null(await world.World.Items.GetByIdAsync(file.ItemId));
        var updatedSpace = await world.World.Spaces.GetByIdAsync(file.SpaceId);
        Assert.Equal(0, updatedSpace!.UsedBytes);
    }

    [Fact]
    public async Task Purge_ByStranger_Denied()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        world.World.DeepFile.Trash(world.World.Owner.UserId);
        await world.World.Db.SaveChangesAsync();

        var result = await world.PurgeItem(world.World.Stranger).Handle(
            new PurgeDriveItemCommand(world.World.DeepFile.ItemId), CancellationToken.None);
        Assert.Equal("Drive.AccessDenied", result.Error.Code);
    }

    [Fact]
    public async Task Purge_NotTrashed_Denied()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var result = await world.PurgeItem(world.World.Admin, isAdmin: true).Handle(
            new PurgeDriveItemCommand(world.World.DeepFile.ItemId), CancellationToken.None);
        Assert.Equal("Drive.ItemNotTrashed", result.Error.Code);
    }

    [Fact]
    public async Task Purge_WithTrackedEditSession_RemovesSessionAndVersions()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var version = await world.World.AddVersionAsync(file, 1, commit: true);
        file.SetCurrentVersion(version.VersionId, 1, 1024, new string('a', 64));
        // A session tracked in the same context, as after an edit-config/save flow: the purge must
        // delete it through the change tracker, not leave it a live dependent of the removed version.
        var session = DriveEditSession.Open(file.ItemId, world.World.Owner.UserId, $"{file.ItemId:N}-1", version.VersionId).Value;
        world.World.Sessions.Add(session);
        file.Trash(world.World.Owner.UserId);
        var space = await world.World.Spaces.GetByIdAsync(file.SpaceId);
        space!.AddUsage(1024, 5L * 1024 * 1024 * 1024);
        await world.World.Db.SaveChangesAsync();
        world.Storage.PutObject(version.ObjectKey, new byte[1024]);

        var result = await world.PurgeItem(world.World.Admin, isAdmin: true).Handle(
            new PurgeDriveItemCommand(file.ItemId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(await world.World.Items.GetByIdAsync(file.ItemId));
        Assert.Null(await world.World.Sessions.GetByIdAsync(session.SessionId));
        Assert.False(world.Storage.Exists(version.ObjectKey));
        Assert.Equal(0, (await world.World.Spaces.GetByIdAsync(file.SpaceId))!.UsedBytes);
    }
}