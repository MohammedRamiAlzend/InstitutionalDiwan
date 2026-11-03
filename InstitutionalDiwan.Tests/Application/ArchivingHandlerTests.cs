using InstitutionalDiwan.Application.Features.Archiving;
using InstitutionalDiwan.Application.Features.Attachments;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Application.Features.Numbering.Numbers;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ArchivingHandlerTests
{
    private const int Location = ArchiveLocation.PhysicalArchiveId;

    private sealed class World
    {
        public FakeUserRepository Users { get; } = new();
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentNumberRepository Numbers { get; } = new();
        public FakeNumberingRegistryRepository Registries { get; } = new();
        public FakeDepartmentRepository Departments { get; } = new();
        public FakeExternalOrganizationRepository ExternalOrganizations { get; } = new();
        public FakeDocumentMovementRepository Movements { get; } = new();
        public FakeDocumentArchiveRepository Archive { get; } = new();
        public FakeLookupRepository Lookups { get; } = new();
        public FakeDocumentAccessEvaluator Access { get; } = new();
        public FakeDepartmentScopeEvaluator Scope { get; } = new();
        public FakeDocumentFileRepository Files { get; } = new();
        public FakeDocumentFileTypeRepository FileTypes { get; } = new FakeDocumentFileTypeRepository().WithDefaultTypes();
        public FakeFileStorageProviderRepository Providers { get; } = new FakeFileStorageProviderRepository().WithLocalProvider();
        public FakeFileSignatureValidator Signatures { get; } = new();
        public FakeFileStorageService Storage { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();

        public User Clerk { get; }
        public Document Document { get; }

        public World()
        {
            Clerk = User.Create("clerk", "hash", Guid.NewGuid()).Value;
            Users.Add(Clerk);

            Document = Document.CreateDraft(
                "طلب إحاطة", DocumentType.InternalMemoId, DocumentDirection.InternalId,
                Clerk.PrimaryDepartmentId, Clerk.UserId).Value;
            Documents.Add(Document);
            Access.ManagedDocuments.Add(Document.DocumentId);

            Lookups.Add("archive-locations", ArchiveLocation.Create(Location, "Physical", "ورقي").Value);
            Lookups.Add("participant-roles", ParticipantRole.Create(ParticipantRole.SenderId, "Sender", "مرسل").Value);
            Registries.Add(NumberingRegistry.Create("DEFAULT", "السجل الافتراضي").Value);
        }

        public ArchiveDocumentCommandHandler ArchiveHandler() =>
            new(Documents, Movements, Archive, Lookups, Access, UnitOfWork);

        public UnarchiveDocumentCommandHandler UnarchiveHandler() =>
            new(Documents, Archive, Access, UnitOfWork);

        public ListDocumentArchiveEventsQueryHandler EventsHandler() =>
            new(Documents, Archive, Access);

        public AttachmentUploader Uploader() => new(Providers, Signatures, Storage);

        /// <summary>Gives the draft its official number, which also moves it to REGISTERED.</summary>
        public async Task<DocumentNumberResponse> RegisterAsync()
        {
            var result = await new IssueOfficialNumberCommandHandler(
                    Documents, Numbers, Registries, new FakeDocumentAccessEvaluator { AllowAll = true }, UnitOfWork, TestClock.Instance)
                .Handle(new IssueOfficialNumberCommand(Document.DocumentId, Clerk.UserId, 2026), CancellationToken.None);
            Assert.True(result.IsSuccess, result.Error?.Code);
            return result.Value;
        }

        public Task<Result<DocumentResponse>> ArchiveAsync(int location = Location, string? reason = null) =>
            ArchiveHandler().Handle(
                new ArchiveDocumentCommand(Document.DocumentId, Clerk.UserId, location, reason),
                CancellationToken.None);

        public Task<Result<DocumentResponse>> UnarchiveAsync(string reason = "مراجعة") =>
            UnarchiveHandler().Handle(
                new UnarchiveDocumentCommand(Document.DocumentId, Clerk.UserId, reason),
                CancellationToken.None);

        public async Task<DocumentFileResponse> CreateFileAsync()
        {
            var result = await CreateFile().Handle(CreateFileCommand(), CancellationToken.None);
            Assert.True(result.IsSuccess, result.Error?.Code);
            return result.Value;
        }

        public CreateDocumentFileCommandHandler CreateFile() =>
            new(Documents, FileTypes, Files, Users, Access, UnitOfWork);

        public CreateDocumentFileCommand CreateFileCommand() =>
            new(Document.DocumentId, DocumentFileType.AttachmentId, "مرفق", Clerk.UserId);
    }

    private static void AssertFrozen(Error error) =>
        Assert.Equal(DomainErrors.Correspondence.DocumentArchived.Code, error.Code);

    [Fact]
    public async Task Archive_RegisteredDocument_ChangesStatusAndRecordsEventInOneSave()
    {
        var world = new World();
        await world.RegisterAsync();
        var savesBefore = world.UnitOfWork.SaveChangesCallCount;

        var result = await world.ArchiveAsync(reason: "انتهاء المعاملة");

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(DocumentStatusType.ArchivedId, result.Value.StatusId);
        Assert.Equal(savesBefore + 1, world.UnitOfWork.SaveChangesCallCount);

        var archiveEvent = Assert.Single(world.Archive.Events);
        Assert.Equal(ArchiveEventType.ArchivedId, archiveEvent.EventTypeId);
        Assert.Equal(DocumentStatusType.RegisteredId, archiveEvent.PreviousStatusId);
        Assert.Equal(DocumentStatusType.ArchivedId, archiveEvent.NewStatusId);
        Assert.Equal(world.Clerk.UserId, archiveEvent.ActorUserId);
        Assert.Equal(Location, archiveEvent.ArchiveLocationId);
        Assert.Equal("انتهاء المعاملة", archiveEvent.Reason);
    }

    [Fact]
    public async Task Archive_UnknownDocument_IsNotFound()
    {
        var world = new World();

        var result = await world.ArchiveHandler().Handle(
            new ArchiveDocumentCommand(Guid.NewGuid(), world.Clerk.UserId, Location), CancellationToken.None);

        Assert.Equal(DomainErrors.Correspondence.DocumentNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Archive_WithViewAccessOnly_IsDeniedBeforeAnyChange()
    {
        var world = new World();
        await world.RegisterAsync();
        world.Access.ManagedDocuments.Clear();
        world.Access.ViewableDocuments.Add(world.Document.DocumentId);

        var result = await world.ArchiveAsync();

        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, result.Error.Code);
        Assert.Equal(DocumentStatusType.RegisteredId, world.Document.CurrentStatusId);
        Assert.Empty(world.Archive.Events);
    }

    [Fact]
    public async Task Archive_UnknownLocation_IsRejected()
    {
        var world = new World();
        await world.RegisterAsync();

        var result = await world.ArchiveAsync(location: ArchiveLocation.ElectronicArchiveId);

        Assert.Equal(DomainErrors.Archiving.LocationNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Archive_InactiveLocation_IsRejected()
    {
        var world = new World();
        await world.RegisterAsync();
        var location = await world.Lookups.GetByIdAsync("archive-locations", Location);
        location!.Deactivate();

        var result = await world.ArchiveAsync();

        Assert.Equal(DomainErrors.Archiving.LocationNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Archive_WithActiveMovement_IsRejected()
    {
        var world = new World();
        await world.RegisterAsync();
        world.Movements.DocumentIdsWithActiveMovements.Add(world.Document.DocumentId);

        var result = await world.ArchiveAsync();

        Assert.Equal(DomainErrors.Correspondence.HasActiveMovements.Code, result.Error.Code);
        Assert.Empty(world.Archive.Events);
    }

    [Fact]
    public async Task Archive_Draft_IsRejected()
    {
        var world = new World();

        var result = await world.ArchiveAsync();

        Assert.Equal(DomainErrors.Correspondence.OnlyRegisteredArchivable.Code, result.Error.Code);
        Assert.Empty(world.Archive.Events);
    }

    [Fact]
    public async Task Unarchive_RestoresStatusAndRecordsEvent()
    {
        var world = new World();
        await world.RegisterAsync();
        await world.ArchiveAsync();

        var result = await world.UnarchiveAsync("طلب قضائي");

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(DocumentStatusType.RegisteredId, result.Value.StatusId);

        var archiveEvent = world.Archive.Events.Last();
        Assert.Equal(ArchiveEventType.UnarchivedId, archiveEvent.EventTypeId);
        Assert.Equal(DocumentStatusType.ArchivedId, archiveEvent.PreviousStatusId);
        Assert.Equal(DocumentStatusType.RegisteredId, archiveEvent.NewStatusId);
        Assert.Equal("طلب قضائي", archiveEvent.Reason);
        Assert.Null(archiveEvent.ArchiveLocationId);
    }

    [Fact]
    public async Task Unarchive_WithoutManageAccess_IsDenied()
    {
        var world = new World();
        await world.RegisterAsync();
        await world.ArchiveAsync();
        world.Access.ManagedDocuments.Clear();

        var result = await world.UnarchiveAsync();

        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, result.Error.Code);
        Assert.Equal(DocumentStatusType.ArchivedId, world.Document.CurrentStatusId);
    }

    [Fact]
    public async Task Unarchive_DocumentNotArchived_IsRejected()
    {
        var world = new World();
        await world.RegisterAsync();

        var result = await world.UnarchiveAsync();

        Assert.Equal(DomainErrors.Correspondence.NotArchived.Code, result.Error.Code);
        Assert.Empty(world.Archive.Events);
    }

    [Fact]
    public async Task Events_AreReturnedToAViewer()
    {
        var world = new World();
        await world.RegisterAsync();
        await world.ArchiveAsync();
        world.Access.ManagedDocuments.Clear();
        world.Access.ViewableDocuments.Add(world.Document.DocumentId);

        var result = await world.EventsHandler().Handle(
            new ListDocumentArchiveEventsQuery(world.Document.DocumentId), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(ArchiveEventType.ArchivedId, Assert.Single(result.Value).EventTypeId);
    }

    [Fact]
    public async Task Events_WithoutAccess_AreDenied()
    {
        var world = new World();
        world.Access.ManagedDocuments.Clear();

        var result = await world.EventsHandler().Handle(
            new ListDocumentArchiveEventsQuery(world.Document.DocumentId), CancellationToken.None);

        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task Events_UnknownDocument_IsNotFound()
    {
        var result = await new World().EventsHandler().Handle(
            new ListDocumentArchiveEventsQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(DomainErrors.Correspondence.DocumentNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Search_ScopesToTheCallerAndMapsRows()
    {
        var world = new World();
        await world.RegisterAsync();
        await world.ArchiveAsync();
        var department = Guid.NewGuid();
        world.Scope.AccessibleDepartments.Add(department);
        var archivedAt = TestClock.Instance.UtcNow;
        world.Archive.Rows.Add(new ArchivedDocumentRow(world.Document, archivedAt, world.Clerk.UserId, Location));
        var caller = new FakeUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() };

        var result = await new SearchArchivedDocumentsQueryHandler(world.Archive, world.Scope, caller, TestClock.Instance)
            .Handle(new SearchArchivedDocumentsQuery(ArchiveLocationId: Location), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        var filter = world.Archive.LastFilter!;
        Assert.Equal(caller.UserId, filter.CallerUserId);
        Assert.Equal(new[] { department }, filter.AccessibleDepartmentIds);
        Assert.Equal(TestClock.Instance.UtcNow, filter.AsOfUtc);
        Assert.Equal(Location, filter.ArchiveLocationId);

        var row = Assert.Single(result.Value.Items);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Equal(world.Document.DocumentId, row.Document.DocumentId);
        Assert.Equal(archivedAt, row.ArchivedAt);
        Assert.Equal(Location, row.ArchiveLocationId);
    }

    [Fact]
    public async Task ArchivedDocument_RejectsNewParticipant()
    {
        var world = new World();
        await world.RegisterAsync();
        await world.ArchiveAsync();

        var result = await new AddDocumentParticipantCommandHandler(
                world.Documents, world.Departments, world.ExternalOrganizations, world.Lookups,
                new FakeDocumentAccessEvaluator { AllowAll = true }, world.UnitOfWork)
            .Handle(
                new AddDocumentParticipantCommand(world.Document.DocumentId, ParticipantRole.SenderId, PersonName: "أحمد"),
                CancellationToken.None);

        AssertFrozen(result.Error);
        Assert.Empty(world.Document.Participants);
    }

    [Fact]
    public async Task ArchivedDocument_RejectsCancellingItsNumber_UntilUnarchived()
    {
        var world = new World();
        var number = await world.RegisterAsync();
        await world.ArchiveAsync();
        var handler = new CancelOfficialNumberCommandHandler(
            world.Documents, world.Numbers, new FakeDocumentAccessEvaluator { AllowAll = true }, world.UnitOfWork);
        var command = new CancelOfficialNumberCommand(world.Document.DocumentId, number.DocumentNumberId, world.Clerk.UserId, "خطأ");

        AssertFrozen((await handler.Handle(command, CancellationToken.None)).Error);

        await world.UnarchiveAsync();

        var result = await handler.Handle(command, CancellationToken.None);
        Assert.True(result.IsSuccess, result.Error?.Code);
    }

    [Fact]
    public async Task ArchivedDocument_RejectsReplacingItsNumber_WithoutConsumingANewOne()
    {
        var world = new World();
        var number = await world.RegisterAsync();
        await world.ArchiveAsync();

        var result = await new ReplaceOfficialNumberCommandHandler(
                world.Documents, world.Numbers, world.Registries,
                new FakeDocumentAccessEvaluator { AllowAll = true }, world.UnitOfWork, TestClock.Instance)
            .Handle(
                new ReplaceOfficialNumberCommand(world.Document.DocumentId, number.DocumentNumberId, world.Clerk.UserId, "تصحيح"),
                CancellationToken.None);

        AssertFrozen(result.Error);
        Assert.Single(world.Numbers.DocumentNumbers);
    }

    [Fact]
    public async Task ArchivedDocument_RejectsDepartmentRegistration()
    {
        var world = new World();
        await world.RegisterAsync();
        await world.ArchiveAsync();

        var result = await new RecordDepartmentRegistrationCommandHandler(
                world.Documents, world.Numbers, world.Departments, world.Registries,
                new FakeDepartmentScopeEvaluator { InstitutionWide = true }, world.UnitOfWork)
            .Handle(
                new RecordDepartmentRegistrationCommand(
                    world.Document.DocumentId, Guid.NewGuid(), "45", TestClock.Instance.UtcNow, world.Clerk.UserId),
                CancellationToken.None);

        AssertFrozen(result.Error);
        Assert.Single(world.Numbers.DocumentNumbers);
    }

    [Fact]
    public async Task ArchivedDocument_RejectsNewFile()
    {
        var world = new World();
        await world.RegisterAsync();
        await world.ArchiveAsync();

        var result = await world.CreateFile().Handle(world.CreateFileCommand(), CancellationToken.None);

        AssertFrozen(result.Error);
        Assert.Empty(world.Files.Files);
    }

    [Fact]
    public async Task ArchivedDocument_RejectsNewFileWithVersion()
    {
        var world = new World();
        await world.RegisterAsync();
        await world.ArchiveAsync();
        var content = new byte[] { 1, 2, 3 };

        var result = await new CreateDocumentFileWithVersionCommandHandler(
                world.Documents, world.FileTypes, world.Files, world.Users, world.Access, world.Uploader(), world.UnitOfWork)
            .Handle(
                new CreateDocumentFileWithVersionCommand(
                    world.Document.DocumentId, DocumentFileType.ScanId, "نسخة", world.Clerk.UserId,
                    new MemoryStream(content), "scan.pdf", "application/pdf", content.Length),
                CancellationToken.None);

        AssertFrozen(result.Error);
        Assert.Empty(world.Files.Files);
    }

    [Fact]
    public async Task ArchivedDocument_RejectsNewFileVersion()
    {
        var world = new World();
        await world.RegisterAsync();
        var file = await world.CreateFileAsync();
        await world.ArchiveAsync();
        var content = new byte[] { 1, 2, 3 };

        var result = await new UploadDocumentFileVersionCommandHandler(
                world.Documents, world.Files, world.FileTypes, world.Users, world.Access, world.Uploader(), world.UnitOfWork)
            .Handle(
                new UploadDocumentFileVersionCommand(
                    file.FileId, world.Clerk.UserId, new MemoryStream(content), "scan.pdf", "application/pdf", content.Length),
                CancellationToken.None);

        AssertFrozen(result.Error);
        Assert.Empty(world.Files.Files.Single().Versions);
    }

    [Fact]
    public async Task ArchivedDocument_RejectsArchivingAFile_UntilUnarchived()
    {
        var world = new World();
        await world.RegisterAsync();
        var file = await world.CreateFileAsync();
        await world.ArchiveAsync();
        var handler = new ArchiveDocumentFileCommandHandler(
            world.Documents, world.Files, world.Users, world.Access, world.UnitOfWork);
        var command = new ArchiveDocumentFileCommand(file.FileId, world.Clerk.UserId);

        AssertFrozen((await handler.Handle(command, CancellationToken.None)).Error);
        Assert.False(world.Files.Files.Single().IsArchived);

        await world.UnarchiveAsync();

        Assert.True((await handler.Handle(command, CancellationToken.None)).IsSuccess);
    }

    [Theory]
    [InlineData(0, null, false)]
    [InlineData(Location, null, true)]
    [InlineData(Location, 500, true)]
    [InlineData(Location, 501, false)]
    public void ArchiveValidator_RequiresLocationAndLimitsReason(int location, int? reasonLength, bool valid)
    {
        var reason = reasonLength is { } length ? new string('س', length) : null;

        var result = new ArchiveDocumentCommandValidator().Validate(
            new ArchiveDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), location, reason));

        Assert.Equal(valid, result.IsValid);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("سبب", true)]
    public void UnarchiveValidator_RequiresReason(string reason, bool valid)
    {
        var result = new UnarchiveDocumentCommandValidator().Validate(
            new UnarchiveDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), reason));

        Assert.Equal(valid, result.IsValid);
    }

    [Fact]
    public void UnarchiveValidator_LimitsReason()
    {
        var result = new UnarchiveDocumentCommandValidator().Validate(
            new UnarchiveDocumentCommand(Guid.NewGuid(), Guid.NewGuid(), new string('س', 501)));

        Assert.Contains(result.Errors, error => error.ErrorCode == "Archiving.ReasonTooLong");
    }
}
