using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DriveDomainTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly Guid SpaceId = Guid.NewGuid();
    private const string Checksum = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    private static DriveSpace NewSharedSpace(long? quota = null) =>
        DriveSpace.Create("مساحة الفريق", DriveSpaceType.SharedId, Owner, quota).Value;

    private static DriveItem NewFolder(Guid? parentId = null) =>
        DriveItem.CreateFolder(SpaceId, parentId, "مجلد", Owner).Value;

    private static DriveItem NewFile(Guid? parentId = null) =>
        DriveItem.CreateFile(SpaceId, parentId, "تقرير.docx", Owner).Value;

    private static DriveFileVersion NewPendingVersion(Guid fileItemId, int number = 1) =>
        DriveFileVersion.CreatePending(
            fileItemId, number, 1, $"{SpaceId}/{fileItemId}/{number}", 1024,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            DriveVersionSource.UploadId, Owner).Value;

    // ── Spaces ──────────────────────────────────────────────────────────────

    [Fact]
    public void CreatePersonal_SucceedsAndRaisesEvent()
    {
        var result = DriveSpace.CreatePersonal(Owner);

        Assert.True(result.IsSuccess);
        Assert.Equal(DriveSpaceType.PersonalId, result.Value.SpaceTypeId);
        Assert.Equal("ملفاتي", result.Value.Name);
        Assert.Equal(0, result.Value.UsedBytes);
        Assert.Contains(result.Value.DomainEvents, e => e is DriveSpaceCreatedEvent);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Create_EmptyName_Fails(string? name)
    {
        var result = DriveSpace.Create(name, DriveSpaceType.SharedId, Owner);
        Assert.Equal(DomainErrors.Drive.SpaceNameEmpty, result.Error);
    }

    [Fact]
    public void Create_NameTooLong_Fails()
    {
        var result = DriveSpace.Create(new string('ن', 151), DriveSpaceType.SharedId, Owner);
        Assert.Equal(DomainErrors.Drive.SpaceNameTooLong, result.Error);
    }

    [Fact]
    public void Create_InvalidType_Fails()
    {
        var result = DriveSpace.Create("س", 99, Owner);
        Assert.Equal(DomainErrors.Drive.SpaceTypeInvalid, result.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_InvalidQuota_Fails(long quota)
    {
        var result = DriveSpace.Create("س", DriveSpaceType.SharedId, Owner, quota);
        Assert.Equal(DomainErrors.Drive.QuotaInvalid, result.Error);
    }

    [Fact]
    public void SetQuota_BelowUsage_Fails()
    {
        var space = NewSharedSpace();
        Assert.True(space.AddUsage(500, 1000).IsSuccess);

        var result = space.SetQuota(400);

        Assert.Equal(DomainErrors.Drive.QuotaBelowUsage, result.Error);
    }

    [Fact]
    public void AddUsage_ExceedingQuota_Fails()
    {
        var space = NewSharedSpace();

        var result = space.AddUsage(1001, 1000);

        Assert.Equal(DomainErrors.Drive.QuotaExceeded, result.Error);
        Assert.Equal(0, space.UsedBytes);
    }

    [Fact]
    public void AddAndReleaseUsage_RoundTrip()
    {
        var space = NewSharedSpace();

        Assert.True(space.AddUsage(600, 1000).IsSuccess);
        Assert.True(space.ReleaseUsage(200).IsSuccess);

        Assert.Equal(400, space.UsedBytes);
    }

    [Fact]
    public void ReleaseUsage_MoreThanUsed_Fails()
    {
        var space = NewSharedSpace();
        Assert.True(space.AddUsage(100, 1000).IsSuccess);

        var result = space.ReleaseUsage(101);

        Assert.Equal(DomainErrors.Drive.UsageInvalid, result.Error);
    }

    [Fact]
    public void Trash_PersonalSpace_IsForbidden()
    {
        var space = DriveSpace.CreatePersonal(Owner).Value;

        var result = space.Trash(Actor);

        Assert.Equal(DomainErrors.Drive.CannotTrashRoot, result.Error);
    }

    [Fact]
    public void TrashAndRestore_SharedSpace_RoundTrip()
    {
        var space = NewSharedSpace();

        Assert.True(space.Trash(Actor).IsSuccess);
        Assert.True(space.IsTrashed);
        Assert.Equal(DomainErrors.Drive.SpaceAlreadyTrashed, space.Trash(Actor).Error);
        Assert.Equal(DomainErrors.Drive.SpaceTrashed, space.Rename("جديد").Error);

        Assert.True(space.Restore().IsSuccess);
        Assert.False(space.IsTrashed);
        Assert.Equal(DomainErrors.Drive.SpaceNotTrashed, space.Restore().Error);
    }

    // ── Items ───────────────────────────────────────────────────────────────

    [Fact]
    public void CreateFolder_AndFile_Succeed()
    {
        var folder = NewFolder();
        var file = NewFile(folder.ItemId);

        Assert.True(folder.IsFolder);
        Assert.True(file.IsFile);
        Assert.Equal(folder.ItemId, file.ParentId);
        Assert.Null(file.CurrentVersionId);
    }

    [Theory]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("a:b")]
    [InlineData("a*b")]
    [InlineData("a?b")]
    [InlineData("a\"b")]
    [InlineData("a<b")]
    [InlineData("a>b")]
    [InlineData("a|b")]
    [InlineData(".")]
    [InlineData("..")]
    public void Create_InvalidName_Fails(string name)
    {
        var result = DriveItem.CreateFile(SpaceId, null, name, Owner);
        Assert.Equal(DomainErrors.Drive.ItemNameInvalid, result.Error);
    }

    [Fact]
    public void CreateItem_EmptyName_Fails()
    {
        var result = DriveItem.CreateFolder(SpaceId, null, "  ", Owner);
        Assert.Equal(DomainErrors.Drive.ItemNameEmpty, result.Error);
    }

    [Fact]
    public void CreateItem_NameTooLong_Fails()
    {
        var result = DriveItem.CreateFile(SpaceId, null, new string('n', 256), Owner);
        Assert.Equal(DomainErrors.Drive.ItemNameTooLong, result.Error);
    }

    [Fact]
    public void Create_BeyondDepthLimit_Fails()
    {
        var result = DriveItem.CreateFolder(SpaceId, Guid.NewGuid(), "عميق", Owner, depth: 31);
        Assert.Equal(DomainErrors.Drive.DepthLimitReached, result.Error);
    }

    [Fact]
    public void Move_IntoSelf_Fails()
    {
        var folder = NewFolder();

        var result = folder.Move(SpaceId, folder.ItemId, Array.Empty<Guid>(), 2);

        Assert.Equal(DomainErrors.Drive.CycleDetected, result.Error);
    }

    [Fact]
    public void Move_IntoDescendant_Fails()
    {
        var folder = NewFolder();

        var result = folder.Move(SpaceId, Guid.NewGuid(), new[] { folder.ItemId }, 3);

        Assert.Equal(DomainErrors.Drive.CycleDetected, result.Error);
    }

    [Fact]
    public void Move_BeyondDepthLimit_Fails()
    {
        var folder = NewFolder();

        var result = folder.Move(SpaceId, Guid.NewGuid(), Array.Empty<Guid>(), 31);

        Assert.Equal(DomainErrors.Drive.DepthLimitReached, result.Error);
    }

    [Fact]
    public void Move_Valid_UpdatesSpaceAndParent()
    {
        var folder = NewFolder();
        var newSpace = Guid.NewGuid();
        var newParent = Guid.NewGuid();

        var result = folder.Move(newSpace, newParent, Array.Empty<Guid>(), 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(newSpace, folder.SpaceId);
        Assert.Equal(newParent, folder.ParentId);
    }

    [Fact]
    public void Rename_TrashedItem_Fails()
    {
        var file = NewFile();
        Assert.True(file.Trash(Actor).IsSuccess);

        var result = file.Rename("جديد.docx");

        Assert.Equal(DomainErrors.Drive.ItemTrashed, result.Error);
    }

    [Fact]
    public void SetCurrentVersion_OnFolder_Fails()
    {
        var folder = NewFolder();

        var result = folder.SetCurrentVersion(Guid.NewGuid(), 1, 100, Checksum);

        Assert.Equal(DomainErrors.Drive.NotAFile, result.Error);
    }

    [Fact]
    public void SetCurrentVersion_Valid_RaisesCommittedEvent()
    {
        var file = NewFile();
        var versionId = Guid.NewGuid();

        var result = file.SetCurrentVersion(versionId, 3, 2048, Checksum);

        Assert.True(result.IsSuccess);
        Assert.Equal(versionId, file.CurrentVersionId);
        Assert.Equal(2048, file.SizeBytes);
        var committed = file.DomainEvents.OfType<DriveFileVersionCommittedEvent>().Single();
        Assert.Equal(3, committed.VersionNumber);
        Assert.Equal(Checksum, committed.ChecksumSha256);
    }

    [Fact]
    public void SetCurrentVersion_BadChecksum_Fails()
    {
        var file = NewFile();

        var result = file.SetCurrentVersion(Guid.NewGuid(), 1, 100, "short");

        Assert.Equal(DomainErrors.Drive.ChecksumInvalid, result.Error);
    }

    [Fact]
    public void TrashAndRestore_Item_RoundTripWithEvents()
    {
        var file = NewFile();

        Assert.True(file.Trash(Actor).IsSuccess);
        Assert.True(file.IsTrashed);
        Assert.Equal(Actor, file.TrashedByUserId);
        Assert.Equal(DomainErrors.Drive.ItemAlreadyTrashed, file.Trash(Actor).Error);
        Assert.Contains(file.DomainEvents, e => e is DriveItemTrashedEvent);

        Assert.True(file.Restore().IsSuccess);
        Assert.False(file.IsTrashed);
        Assert.Null(file.TrashedByUserId);
        Assert.Equal(DomainErrors.Drive.ItemNotTrashed, file.Restore().Error);
        Assert.Contains(file.DomainEvents, e => e is DriveItemRestoredEvent);
    }

    [Fact]
    public void BreakAndRestoreInheritance_RoundTrip()
    {
        var folder = NewFolder();

        Assert.True(folder.BreakPermissionInheritance().IsSuccess);
        Assert.True(folder.BreakInheritance);
        Assert.True(folder.RestorePermissionInheritance().IsSuccess);
        Assert.False(folder.BreakInheritance);
        Assert.Equal(DomainErrors.Drive.InheritanceBroken, folder.RestorePermissionInheritance().Error);
    }

    // ── Versions ────────────────────────────────────────────────────────────

    [Fact]
    public void CreatePending_Valid()
    {
        var file = NewFile();

        var version = NewPendingVersion(file.ItemId);

        Assert.True(version.IsPending);
        Assert.Null(version.ChecksumSha256);
        Assert.Equal(1024, version.SizeBytes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreatePending_InvalidNumber_Fails(int number)
    {
        var result = DriveFileVersion.CreatePending(
            Guid.NewGuid(), number, 1, "key", 10, null, DriveVersionSource.UploadId, Owner);
        Assert.Equal(DomainErrors.Drive.VersionNumberInvalid, result.Error);
    }

    [Fact]
    public void Commit_Valid_SetsFields()
    {
        var version = NewPendingVersion(NewFile().ItemId);

        var result = version.Commit(Checksum.ToUpperInvariant(), 2048);

        Assert.True(result.IsSuccess);
        Assert.True(version.IsCommitted);
        Assert.Equal(Checksum, version.ChecksumSha256);
        Assert.Equal(2048, version.SizeBytes);
        Assert.NotNull(version.CommittedAt);
    }

    [Fact]
    public void Commit_BadChecksum_Fails()
    {
        var version = NewPendingVersion(NewFile().ItemId);

        var result = version.Commit("zz", 100);

        Assert.Equal(DomainErrors.Drive.ChecksumInvalid, result.Error);
        Assert.True(version.IsPending);
    }

    [Fact]
    public void Commit_Twice_Fails()
    {
        var version = NewPendingVersion(NewFile().ItemId);
        Assert.True(version.Commit(Checksum, 100).IsSuccess);

        var result = version.Commit(Checksum, 100);

        Assert.Equal(DomainErrors.Drive.VersionNotPending, result.Error);
    }

    [Fact]
    public void Abandon_AfterCommit_Fails()
    {
        var version = NewPendingVersion(NewFile().ItemId);
        Assert.True(version.Commit(Checksum, 100).IsSuccess);

        Assert.Equal(DomainErrors.Drive.VersionNotPending, version.Abandon().Error);
    }

    [Fact]
    public void Abandon_Pending_Succeeds()
    {
        var version = NewPendingVersion(NewFile().ItemId);

        Assert.True(version.Abandon().IsSuccess);
        Assert.Equal(DriveVersionStatus.AbandonedId, version.StatusId);
    }

    // ── Permissions ─────────────────────────────────────────────────────────

    [Fact]
    public void CreatePermission_Valid()
    {
        var principal = Guid.NewGuid();

        var result = DrivePermission.Create(NewFolder().ItemId, DrivePrincipalType.DepartmentId, principal, DriveRole.EditorId, Actor);

        Assert.True(result.IsSuccess);
        Assert.Equal(principal, result.Value.PrincipalId);
        Assert.Equal(DriveRole.EditorId, result.Value.RoleId);
    }

    [Fact]
    public void CreatePermission_InvalidRole_Fails()
    {
        var result = DrivePermission.Create(Guid.NewGuid(), DrivePrincipalType.UserId, Guid.NewGuid(), 9, Actor);
        Assert.Equal(DomainErrors.Drive.RoleInvalid, result.Error);
    }

    [Fact]
    public void CreatePermission_InvalidPrincipalType_Fails()
    {
        var result = DrivePermission.Create(Guid.NewGuid(), 5, Guid.NewGuid(), DriveRole.ViewerId, Actor);
        Assert.Equal(DomainErrors.Drive.PrincipalTypeInvalid, result.Error);
    }

    [Fact]
    public void ChangeRole_ValidAndInvalid()
    {
        var permission = DrivePermission.Create(
            Guid.NewGuid(), DrivePrincipalType.UserId, Guid.NewGuid(), DriveRole.ViewerId, Actor).Value;

        Assert.True(permission.ChangeRole(DriveRole.ManagerId).IsSuccess);
        Assert.Equal(DriveRole.ManagerId, permission.RoleId);
        Assert.Equal(DomainErrors.Drive.RoleInvalid, permission.ChangeRole(0).Error);
    }

    // ── Deletion requests ───────────────────────────────────────────────────

    [Fact]
    public void CreateRequest_Valid_RaisesRequestedEvent()
    {
        var itemId = Guid.NewGuid();

        var result = DriveDeletionRequest.Create(itemId, Owner, "لم تعد هناك حاجة للملف");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsPending);
        var requested = result.Value.DomainEvents.OfType<DriveDeletionRequestedEvent>().Single();
        Assert.Equal(itemId, requested.ItemId);
        Assert.Equal(Owner, requested.RequestedByUserId);
    }

    [Fact]
    public void CreateRequest_EmptyReason_Fails()
    {
        var result = DriveDeletionRequest.Create(Guid.NewGuid(), Owner, "  ");
        Assert.Equal(DomainErrors.Drive.RequestReasonEmpty, result.Error);
    }

    [Fact]
    public void CreateRequest_ReasonTooLong_Fails()
    {
        var result = DriveDeletionRequest.Create(Guid.NewGuid(), Owner, new string('س', 501));
        Assert.Equal(DomainErrors.Drive.RequestReasonTooLong, result.Error);
    }

    [Fact]
    public void Approve_Pending_SucceedsAndRaisesEvent()
    {
        var request = DriveDeletionRequest.Create(Guid.NewGuid(), Owner, "سبب").Value;

        var result = request.Approve(Actor, "موافق");

        Assert.True(result.IsSuccess);
        Assert.Equal(DriveDeletionRequestStatus.ApprovedId, request.StatusId);
        Assert.Equal(Actor, request.ReviewedByUserId);
        Assert.Equal("موافق", request.ReviewNote);
        Assert.Contains(request.DomainEvents, e => e is DriveDeletionApprovedEvent);
    }

    [Fact]
    public void Approve_Twice_Fails()
    {
        var request = DriveDeletionRequest.Create(Guid.NewGuid(), Owner, "سبب").Value;
        Assert.True(request.Approve(Actor).IsSuccess);

        Assert.Equal(DomainErrors.Drive.RequestAlreadyReviewed, request.Approve(Actor).Error);
        Assert.Equal(DomainErrors.Drive.RequestAlreadyReviewed, request.Reject(Actor).Error);
    }

    [Fact]
    public void Reject_Pending_RaisesRejectedEvent()
    {
        var request = DriveDeletionRequest.Create(Guid.NewGuid(), Owner, "سبب").Value;

        Assert.True(request.Reject(Actor, "ما زال مطلوبًا").IsSuccess);

        Assert.Equal(DriveDeletionRequestStatus.RejectedId, request.StatusId);
        Assert.Contains(request.DomainEvents, e => e is DriveDeletionRejectedEvent);
    }

    [Fact]
    public void ReviewNote_TooLong_Fails()
    {
        var request = DriveDeletionRequest.Create(Guid.NewGuid(), Owner, "سبب").Value;

        var result = request.Approve(Actor, new string('ن', 501));

        Assert.Equal(DomainErrors.Drive.ReviewNoteTooLong, result.Error);
        Assert.True(request.IsPending);
    }

    [Fact]
    public void Cancel_ByOtherUser_IsForbidden()
    {
        var request = DriveDeletionRequest.Create(Guid.NewGuid(), Owner, "سبب").Value;

        Assert.Equal(DomainErrors.Drive.OnlyRequesterCanCancel, request.Cancel(Actor).Error);
    }

    [Fact]
    public void Cancel_ByRequester_Succeeds()
    {
        var request = DriveDeletionRequest.Create(Guid.NewGuid(), Owner, "سبب").Value;

        Assert.True(request.Cancel(Owner).IsSuccess);
        Assert.Equal(DriveDeletionRequestStatus.CancelledId, request.StatusId);
        Assert.Equal(DomainErrors.Drive.RequestAlreadyReviewed, request.Cancel(Owner).Error);
    }

    // ── Edit sessions ───────────────────────────────────────────────────────

    [Fact]
    public void OpenSession_Valid()
    {
        var result = DriveEditSession.Open(Guid.NewGuid(), Owner, "item-1", Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsOpen);
        Assert.Equal("item-1", result.Value.DocumentKey);
    }

    [Fact]
    public void OpenSession_EmptyKey_Fails()
    {
        var result = DriveEditSession.Open(Guid.NewGuid(), Owner, " ", Guid.NewGuid());
        Assert.Equal(DomainErrors.Drive.DocumentKeyEmpty, result.Error);
    }

    [Fact]
    public void OpenSession_KeyTooLong_Fails()
    {
        var result = DriveEditSession.Open(Guid.NewGuid(), Owner, new string('k', 201), Guid.NewGuid());
        Assert.Equal(DomainErrors.Drive.DocumentKeyTooLong, result.Error);
    }

    [Fact]
    public void SessionLifecycle_SaveThenClose()
    {
        var session = DriveEditSession.Open(Guid.NewGuid(), Owner, "item-1", Guid.NewGuid()).Value;

        Assert.True(session.Touch().IsSuccess);
        Assert.True(session.MarkSaved().IsSuccess);
        Assert.Equal(DriveEditSessionStatus.SavedId, session.StatusId);
        Assert.Equal(DomainErrors.Drive.SessionNotOpen, session.MarkSaved().Error);
        Assert.True(session.Close().IsSuccess);
        Assert.Equal(DriveEditSessionStatus.ClosedId, session.StatusId);
        Assert.Equal(DomainErrors.Drive.SessionNotOpen, session.Close().Error);
    }

    [Fact]
    public void Expire_OpenSession_Succeeds_ClosedFails()
    {
        var session = DriveEditSession.Open(Guid.NewGuid(), Owner, "item-1", Guid.NewGuid()).Value;

        Assert.True(session.Expire().IsSuccess);
        Assert.Equal(DriveEditSessionStatus.ExpiredId, session.StatusId);
        Assert.Equal(DomainErrors.Drive.SessionNotOpen, session.Expire().Error);
    }

    // ── Stars ───────────────────────────────────────────────────────────────

    [Fact]
    public void CreateStar_Valid()
    {
        var itemId = Guid.NewGuid();

        var result = DriveStar.Create(Owner, itemId);

        Assert.True(result.IsSuccess);
        Assert.Equal(itemId, result.Value.ItemId);
    }

    [Fact]
    public void CreateStar_EmptyIds_Fails()
    {
        Assert.Equal(DomainErrors.Drive.PrincipalIdEmpty, DriveStar.Create(Guid.Empty, Guid.NewGuid()).Error);
        Assert.Equal(DomainErrors.Drive.PrincipalIdEmpty, DriveStar.Create(Owner, Guid.Empty).Error);
    }

    // ── Institution Drive settings ──────────────────────────────────────────

    private static InstitutionSetting NewSetting() =>
        InstitutionSetting.Create("مؤسسة", 1, 1).Value;

    [Fact]
    public void UpdateDriveSettings_Valid_UpdatesAllThree()
    {
        var setting = NewSetting();
        var quota = 10L * 1024 * 1024 * 1024;
        var maxFile = 500L * 1024 * 1024;

        var result = setting.UpdateDriveSettings(quota, maxFile, ".pdf, .docx");

        Assert.True(result.IsSuccess);
        Assert.Equal(quota, setting.DriveDefaultQuotaBytes);
        Assert.Equal(maxFile, setting.DriveMaxFileSizeBytes);
        Assert.Equal(".pdf, .docx", setting.DriveAllowedExtensionsCsv);
    }

    [Fact]
    public void UpdateDriveSettings_Defaults_AreFiveGbAndTwoHundredMb()
    {
        var setting = NewSetting();

        Assert.Equal(5L * 1024 * 1024 * 1024, setting.DriveDefaultQuotaBytes);
        Assert.Equal(200L * 1024 * 1024, setting.DriveMaxFileSizeBytes);
        Assert.False(string.IsNullOrWhiteSpace(setting.DriveAllowedExtensionsCsv));
    }

    [Fact]
    public void UpdateDriveSettings_InvalidQuota_Fails()
    {
        var setting = NewSetting();

        Assert.Equal(DomainErrors.Drive.QuotaInvalid, setting.UpdateDriveSettings(0, 1024, ".pdf").Error);
        Assert.Equal(DomainErrors.Drive.QuotaInvalid, setting.UpdateDriveSettings(-1, 1024, ".pdf").Error);
    }

    [Fact]
    public void UpdateDriveSettings_MaxFileAboveQuotaOrZero_Fails()
    {
        var setting = NewSetting();

        Assert.Equal(DomainErrors.Drive.MaxFileSizeInvalid, setting.UpdateDriveSettings(100, 200, ".pdf").Error);
        Assert.Equal(DomainErrors.Drive.MaxFileSizeInvalid, setting.UpdateDriveSettings(100, 0, ".pdf").Error);
    }

    [Fact]
    public void UpdateDriveSettings_EmptyExtensions_Fails()
    {
        var setting = NewSetting();

        Assert.Equal(DomainErrors.Drive.ExtensionsEmpty, setting.UpdateDriveSettings(1024, 512, "   ").Error);
        Assert.Equal(DomainErrors.Drive.ExtensionsEmpty, setting.UpdateDriveSettings(1024, 512, null).Error);
    }
}