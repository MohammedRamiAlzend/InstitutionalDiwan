using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DriveHandlerTests
{
    [Fact]
    public async Task CreateSpace_Shared_OwnerIsCaller()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var result = await world.CreateSpace(world.World.Member).Handle(
            new CreateDriveSpaceCommand("مساحة جديدة"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(world.World.Member.UserId, result.Value.OwnerUserId);
        Assert.Equal(DriveSpaceType.SharedId, result.Value.SpaceTypeId);
        Assert.Equal(5L * 1024 * 1024 * 1024, result.Value.EffectiveQuotaBytes);
    }

    [Fact]
    public async Task CreateSpace_QuotaOverride_RequiresManage()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var denied = await world.CreateSpace(world.World.Member).Handle(
            new CreateDriveSpaceCommand("مساحة", 1024), CancellationToken.None);
        Assert.True(denied.IsFailure);
        Assert.Equal("Drive.AccessDenied", denied.Error.Code);

        var allowed = await world.CreateSpace(world.World.Admin, isAdmin: true).Handle(
            new CreateDriveSpaceCommand("مساحة", 1024), CancellationToken.None);
        Assert.True(allowed.IsSuccess);
        Assert.Equal(1024, allowed.Value.QuotaBytes);
    }

    [Fact]
    public async Task EnsurePersonal_Idempotent()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var first = await world.EnsurePersonal(world.World.Member).Handle(new EnsurePersonalDriveSpaceCommand(), CancellationToken.None);
        var second = await world.EnsurePersonal(world.World.Member).Handle(new EnsurePersonalDriveSpaceCommand(), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value.SpaceId, second.Value.SpaceId);
        Assert.Equal(DriveSpaceType.PersonalId, first.Value.SpaceTypeId);
    }

    [Fact]
    public async Task SetQuota_RequiresManage_AndRejectsBelowUsage()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var denied = await world.SetQuota(world.World.Member).Handle(
            new SetDriveSpaceQuotaCommand(world.World.Shared.SpaceId, 4096), CancellationToken.None);
        Assert.Equal("Drive.AccessDenied", denied.Error.Code);

        world.World.Shared.AddUsage(2048, 5L * 1024 * 1024 * 1024);
        await world.World.Db.SaveChangesAsync();

        var below = await world.SetQuota(world.World.Admin, isAdmin: true).Handle(
            new SetDriveSpaceQuotaCommand(world.World.Shared.SpaceId, 1024), CancellationToken.None);
        Assert.Equal("Drive.QuotaBelowUsage", below.Error.Code);

        var ok = await world.SetQuota(world.World.Admin, isAdmin: true).Handle(
            new SetDriveSpaceQuotaCommand(world.World.Shared.SpaceId, 8192), CancellationToken.None);
        Assert.True(ok.IsSuccess);
        Assert.Equal(8192, ok.Value.QuotaBytes);
    }

    [Fact]
    public async Task CreateFolder_AtRoot_OnlyOwnerOrAdmin()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var denied = await world.CreateFolder(world.World.Member).Handle(
            new CreateDriveFolderCommand(world.World.Shared.SpaceId, null, "مجلد"), CancellationToken.None);
        Assert.Equal("Drive.AccessDenied", denied.Error.Code);

        var ok = await world.CreateFolder(world.World.Owner).Handle(
            new CreateDriveFolderCommand(world.World.Shared.SpaceId, null, "مجلد"), CancellationToken.None);
        Assert.True(ok.IsSuccess);
        Assert.Null(ok.Value.ParentId);
    }

    [Fact]
    public async Task CreateFolder_InsideFolder_WithEditorGrant()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        await world.World.AddGrantAsync(world.World.RootFolder.ItemId, DrivePrincipalType.UserId, world.World.Member.UserId, DriveRole.EditorId);

        var ok = await world.CreateFolder(world.World.Member).Handle(
            new CreateDriveFolderCommand(world.World.Shared.SpaceId, world.World.RootFolder.ItemId, "فرعي جديد"), CancellationToken.None);
        Assert.True(ok.IsSuccess);
        Assert.Equal(world.World.RootFolder.ItemId, ok.Value.ParentId);
    }

    [Fact]
    public async Task CreateFolder_NameTaken()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var result = await world.CreateFolder(world.World.Owner).Handle(
            new CreateDriveFolderCommand(world.World.Shared.SpaceId, null, "مشروع"), CancellationToken.None);
        Assert.Equal("Drive.NameTaken", result.Error.Code);
    }

    [Fact]
    public async Task Rename_RequiresEditor()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var denied = await world.RenameItem(world.World.Stranger).Handle(
            new RenameDriveItemCommand(world.World.RootFolder.ItemId, "اسم جديد"), CancellationToken.None);
        Assert.Equal("Drive.AccessDenied", denied.Error.Code);

        var ok = await world.RenameItem(world.World.Owner).Handle(
            new RenameDriveItemCommand(world.World.RootFolder.ItemId, "اسم جديد"), CancellationToken.None);
        Assert.True(ok.IsSuccess);
        Assert.Equal("اسم جديد", ok.Value.Name);
    }

    [Fact]
    public async Task Move_IntoDescendant_CycleDetected()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var result = await world.MoveItem(world.World.Owner).Handle(
            new MoveDriveItemCommand(world.World.RootFolder.ItemId, world.World.ChildFolder.ItemId), CancellationToken.None);
        Assert.Equal("Drive.CycleDetected", result.Error.Code);
    }

    [Fact]
    public async Task Move_CrossSpace_ByOwner_Succeeds()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var target = await world.CreateFolder(world.World.Owner).Handle(
            new CreateDriveFolderCommand(world.World.Personal.SpaceId, null, "هدف"), CancellationToken.None);
        Assert.True(target.IsSuccess);

        var result = await world.MoveItem(world.World.Owner).Handle(
            new MoveDriveItemCommand(world.World.MemberFile.ItemId, target.Value.ItemId), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(world.World.Personal.SpaceId, result.Value.SpaceId);
        Assert.Equal(target.Value.ItemId, result.Value.ParentId);
    }

    [Fact]
    public async Task Move_CrossSpace_WithoutManage_Denied()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var target = await world.CreateFolder(world.World.Owner).Handle(
            new CreateDriveFolderCommand(world.World.Personal.SpaceId, null, "هدف"), CancellationToken.None);
        await world.World.AddGrantAsync(world.World.MemberFile.ItemId, DrivePrincipalType.UserId, world.World.Member.UserId, DriveRole.EditorId);

        var result = await world.MoveItem(world.World.Member).Handle(
            new MoveDriveItemCommand(world.World.MemberFile.ItemId, target.Value.ItemId), CancellationToken.None);
        Assert.Equal("Drive.AccessDenied", result.Error.Code);
    }

    [Fact]
    public async Task Browse_AccessFiltered_ByCaller()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var ownerView = await world.Browse(world.World.Owner).Handle(
            new BrowseDriveItemsQuery(SpaceId: world.World.Shared.SpaceId), CancellationToken.None);
        Assert.True(ownerView.IsSuccess);
        Assert.Equal(4, ownerView.Value.TotalCount);

        var strangerView = await world.Browse(world.World.Stranger).Handle(
            new BrowseDriveItemsQuery(SpaceId: world.World.Shared.SpaceId), CancellationToken.None);
        Assert.Equal(0, strangerView.Value.TotalCount);
    }

    [Fact]
    public async Task Star_Idempotent_AndPersonal()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var handler = world.Star(world.World.Owner);
        var first = await handler.Handle(new StarDriveItemCommand(world.World.RootFolder.ItemId), CancellationToken.None);
        var second = await handler.Handle(new StarDriveItemCommand(world.World.RootFolder.ItemId), CancellationToken.None);
        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);

        var stars = await world.World.Stars.ListByUserAsync(world.World.Owner.UserId);
        Assert.Single(stars);
    }

    [Fact]
    public async Task InitiateUpload_NewFile_ReservesPendingVersion()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var result = await world.InitiateUpload(world.World.Owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "ملف.txt", 1024, "text/plain"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.VersionNumber);
        var version = await world.World.Versions.GetByIdAsync(result.Value.VersionId);
        Assert.True(version!.IsPending);
        Assert.Contains("verb=PUT", result.Value.UploadUrl.ToString());
    }

    [Fact]
    public async Task InitiateUpload_ExtensionNotAllowed()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var result = await world.InitiateUpload(world.World.Owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "virus.exe", 1024), CancellationToken.None);
        Assert.Equal("Drive.ExtensionNotAllowed", result.Error.Code);
    }

    [Fact]
    public async Task InitiateUpload_QuotaExceeded()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        await world.SetQuota(world.World.Admin, isAdmin: true).Handle(
            new SetDriveSpaceQuotaCommand(world.World.Personal.SpaceId, 1024), CancellationToken.None);

        var result = await world.InitiateUpload(world.World.Owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "كبير.txt", 2048), CancellationToken.None);
        Assert.Equal("Drive.QuotaExceeded", result.Error.Code);
    }

    [Fact]
    public async Task CompleteUpload_CommitsVersion_UpdatesUsageAndCurrent()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var initiated = await world.InitiateUpload(world.World.Owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "ملف.txt", 4, "text/plain"), CancellationToken.None);
        var bytes = new byte[] { 1, 2, 3, 4 };
        world.Storage.PutObject(initiated.Value.ObjectKey, bytes);

        var completed = await world.CompleteUpload(world.World.Owner).Handle(
            new CompleteDriveUploadCommand(initiated.Value.VersionId), CancellationToken.None);

        Assert.True(completed.IsSuccess);
        Assert.True(completed.Value.IsCurrent);
        Assert.Equal(DriveVersionStatus.CommittedId, completed.Value.StatusId);

        var file = await world.World.Items.GetByIdAsync(initiated.Value.ItemId);
        Assert.Equal(initiated.Value.VersionId, file!.CurrentVersionId);
        Assert.Equal(4, file.SizeBytes);
        var space = await world.World.Spaces.GetByIdAsync(world.World.Personal.SpaceId);
        Assert.Equal(4, space!.UsedBytes);
    }

    [Fact]
    public async Task CompleteUpload_SizeMismatch()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var initiated = await world.InitiateUpload(world.World.Owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "ملف.txt", 4), CancellationToken.None);
        world.Storage.PutObject(initiated.Value.ObjectKey, new byte[] { 1, 2, 3 });

        var result = await world.CompleteUpload(world.World.Owner).Handle(
            new CompleteDriveUploadCommand(initiated.Value.VersionId), CancellationToken.None);
        Assert.Equal("Drive.DeclaredSizeMismatch", result.Error.Code);
    }

    [Fact]
    public async Task CompleteUpload_SignatureMismatch()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        world.Signatures.Failure = DomainErrors.Attachments.SignatureMismatch;
        var initiated = await world.InitiateUpload(world.World.Owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "ملف.txt", 4), CancellationToken.None);
        world.Storage.PutObject(initiated.Value.ObjectKey, new byte[] { 1, 2, 3, 4 });

        var result = await world.CompleteUpload(world.World.Owner).Handle(
            new CompleteDriveUploadCommand(initiated.Value.VersionId), CancellationToken.None);
        Assert.Equal("Drive.SignatureMismatch", result.Error.Code);
    }

    [Fact]
    public async Task CompleteUpload_ClientChecksumMismatch()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var initiated = await world.InitiateUpload(world.World.Owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "ملف.txt", 4), CancellationToken.None);
        world.Storage.PutObject(initiated.Value.ObjectKey, new byte[] { 1, 2, 3, 4 });

        var result = await world.CompleteUpload(world.World.Owner).Handle(
            new CompleteDriveUploadCommand(initiated.Value.VersionId, new string('b', 64)), CancellationToken.None);
        Assert.Equal("Drive.SignatureMismatch", result.Error.Code);
    }

    [Fact]
    public async Task CompleteUpload_QuotaEnforcedAcrossCommits()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        await world.SetQuota(world.World.Admin, isAdmin: true).Handle(
            new SetDriveSpaceQuotaCommand(world.World.Personal.SpaceId, 8), CancellationToken.None);

        var first = await world.InitiateUpload(world.World.Owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "أ.txt", 6), CancellationToken.None);
        world.Storage.PutObject(first.Value.ObjectKey, new byte[6]);
        var firstComplete = await world.CompleteUpload(world.World.Owner).Handle(
            new CompleteDriveUploadCommand(first.Value.VersionId), CancellationToken.None);
        Assert.True(firstComplete.IsSuccess);

        var second = await world.InitiateUpload(world.World.Owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "ب.txt", 6), CancellationToken.None);
        Assert.Equal("Drive.QuotaExceeded", second.Error.Code);
    }

    [Fact]
    public async Task RestoreVersion_CreatesNewCopy_AndBumpsUsage()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var v1 = await world.World.AddVersionAsync(file, 1, commit: true);
        file.SetCurrentVersion(v1.VersionId, 1, 1024, new string('a', 64));
        await world.World.Db.SaveChangesAsync();
        world.Storage.PutObject(v1.ObjectKey, new byte[1024]);

        var space = await world.World.Spaces.GetByIdAsync(file.SpaceId);
        space!.AddUsage(1024, 5L * 1024 * 1024 * 1024);
        await world.World.Db.SaveChangesAsync();

        var restored = await world.RestoreVersion(world.World.Owner).Handle(
            new RestoreDriveFileVersionCommand(file.ItemId, v1.VersionId), CancellationToken.None);

        Assert.True(restored.IsSuccess);
        Assert.Equal(2, restored.Value.VersionNumber);
        Assert.Equal(DriveVersionSource.RestoreId, restored.Value.SourceId);
        Assert.True(restored.Value.IsCurrent);
        Assert.Equal(v1.ChecksumSha256, restored.Value.ChecksumSha256);

        var updatedSpace = await world.World.Spaces.GetByIdAsync(file.SpaceId);
        Assert.Equal(2048, updatedSpace!.UsedBytes);
    }

    [Fact]
    public async Task Grant_RequiresManage_AndNotifiesGrantee()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var denied = await world.Grant(world.World.Member).Handle(
            new GrantDrivePermissionCommand(world.World.RootFolder.ItemId, DrivePrincipalType.UserId, world.World.Stranger.UserId, DriveRole.ViewerId), CancellationToken.None);
        Assert.Equal("Drive.AccessDenied", denied.Error.Code);

        var ok = await world.Grant(world.World.Owner).Handle(
            new GrantDrivePermissionCommand(world.World.RootFolder.ItemId, DrivePrincipalType.UserId, world.World.Member.UserId, DriveRole.EditorId), CancellationToken.None);
        Assert.True(ok.IsSuccess);
        Assert.Contains(world.Notifications.Published, n =>
            n.TypeId == NotificationType.DrivePermissionGrantedId && n.Recipients.Contains(world.World.Member.UserId));
    }

    [Fact]
    public async Task Grant_Department_NotifiesDepartmentUsers()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var ok = await world.Grant(world.World.Owner).Handle(
            new GrantDrivePermissionCommand(world.World.RootFolder.ItemId, DrivePrincipalType.DepartmentId, world.World.Dept.DepartmentId, DriveRole.ViewerId), CancellationToken.None);

        Assert.True(ok.IsSuccess);
        var notification = world.Notifications.Published.Single(n => n.TypeId == NotificationType.DrivePermissionGrantedId);
        Assert.Contains(world.World.Member.UserId, notification.Recipients);
        Assert.DoesNotContain(world.World.Stranger.UserId, notification.Recipients);
    }

    [Fact]
    public async Task BreakInheritance_IgnoresAncestorGrants()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        await world.World.AddGrantAsync(world.World.RootFolder.ItemId, DrivePrincipalType.UserId, world.World.Member.UserId, DriveRole.EditorId);

        var before = await world.AccessFor(world.World.Member).GetEffectiveRoleAsync(world.World.ChildFolder.ItemId);
        Assert.Equal(DriveRole.EditorId, before);

        var broken = await world.Inheritance(world.World.Owner).Handle(
            new SetDriveItemInheritanceCommand(world.World.ChildFolder.ItemId, true), CancellationToken.None);
        Assert.True(broken.IsSuccess);

        var after = await world.AccessFor(world.World.Member).GetEffectiveRoleAsync(world.World.ChildFolder.ItemId);
        Assert.Equal(0, after);
    }

    [Fact]
    public async Task UpdateSettings_RequiresManage_AndValidates()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var denied = await world.UpdateSettings(world.World.Member).Handle(
            new UpdateDriveSettingsCommand(1024, 512, ".txt"), CancellationToken.None);
        Assert.Equal("Drive.AccessDenied", denied.Error.Code);

        var invalid = await world.UpdateSettings(world.World.Admin, isAdmin: true).Handle(
            new UpdateDriveSettingsCommand(1024, 2048, ".txt"), CancellationToken.None);
        Assert.Equal("Drive.MaxFileSizeInvalid", invalid.Error.Code);

        var ok = await world.UpdateSettings(world.World.Admin, isAdmin: true).Handle(
            new UpdateDriveSettingsCommand(10L * 1024 * 1024 * 1024, 500L * 1024 * 1024, ".txt,.pdf"), CancellationToken.None);
        Assert.True(ok.IsSuccess);
        Assert.Equal(".txt,.pdf", ok.Value.AllowedExtensionsCsv);
    }
}