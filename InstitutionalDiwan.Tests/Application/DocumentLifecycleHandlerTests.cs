using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DocumentLifecycleHandlerTests
{
    private const int NumberingYear = 2026;

    private sealed class World
    {
        public FakeUserRepository Users { get; } = new();
        public FakeDocumentTypeRepository DocumentTypes { get; } = new();
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentNumberRepository Numbers { get; } = new();
        public FakeNumberingRegistryRepository Registries { get; } = new();
        public FakeDocumentMovementRepository Movements { get; } = new();
        public FakeExternalOrganizationRepository ExternalOrganizations { get; } = new();
        public FakeDepartmentRepository Departments { get; } = new();
        public FakeLookupRepository Lookups { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public User User { get; }
        public Guid OwningDepartmentId { get; } = Guid.NewGuid();

        public World()
        {
            var user = User.Create("clerk", "hash", OwningDepartmentId);
            Assert.True(user.IsSuccess);
            User = user.Value;
            Users.Add(User);

            var documentType = DocumentType.Create(DocumentType.InternalMemoId, "Internal memo", "مذكرة داخلية");
            Assert.True(documentType.IsSuccess);
            DocumentTypes.Add(documentType.Value);

            var registry = NumberingRegistry.Create("DEFAULT", "السجل الافتراضي");
            Assert.True(registry.IsSuccess);
            Registries.Add(registry.Value);

            Lookups.Add(
                "participant-roles",
                ParticipantRole.Create(ParticipantRole.SenderId, "Sender", "مرسل").Value);
        }

        public CreateDraftDocumentCommandHandler CreateDraftHandler() =>
            new(Users, Documents, DocumentTypes, UnitOfWork);

        public RegisterDocumentCommandHandler RegisterHandler() =>
            new(Documents, Numbers, Registries, UnitOfWork, TestClock.Instance);

        public UpdateDraftMetadataCommandHandler UpdateHandler() =>
            new(Documents, UnitOfWork);

        public AddDocumentParticipantCommandHandler AddParticipantHandler() =>
            new(Documents, Departments, ExternalOrganizations, Lookups, UnitOfWork);

        public CancelDocumentCommandHandler CancelHandler() =>
            new(Documents, Movements, UnitOfWork);
    }

    private static async Task<DocumentResponse> CreateDraftAsync(World world)
    {
        var result = await world.CreateDraftHandler().Handle(new CreateDraftDocumentCommand(
            world.User.UserId,
            "طلب إحاطة",
            DocumentType.InternalMemoId,
            DocumentDirection.InternalId,
            "ملخص"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        return result.Value;
    }

    [Fact]
    public async Task CreateDraft_PersistsDraftWithoutOfficialNumber()
    {
        var world = new World();

        var draft = await CreateDraftAsync(world);

        Assert.Equal(DocumentStatusType.DraftId, draft.StatusId);
        Assert.Null(draft.OfficialNumberText);
        Assert.Equal(world.OwningDepartmentId, draft.OwningDepartmentId);
        Assert.Single(world.Documents.Documents);
        Assert.Empty(world.Numbers.DocumentNumbers);
    }

    [Fact]
    public async Task Register_DraftIssuesOfficialNumberAndRegisters()
    {
        var world = new World();
        var draft = await CreateDraftAsync(world);

        var result = await world.RegisterHandler().Handle(
            new RegisterDocumentCommand(draft.DocumentId, world.User.UserId, NumberingYear),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentStatusType.RegisteredId, result.Value.StatusId);
        Assert.Equal($"{NumberingYear}/000001", result.Value.OfficialNumberText);
        Assert.NotNull(result.Value.RegistrationDate);

        var number = Assert.Single(world.Numbers.DocumentNumbers);
        Assert.Equal(1, number.SequenceValue);
        Assert.Equal(NumberingYear, number.NumberingYear);
        Assert.Equal(DocumentNumberStatus.ActiveId, number.StatusId);
    }

    [Fact]
    public async Task Register_WhenAlreadyRegistered_ReturnsConflict()
    {
        var world = new World();
        var draft = await CreateDraftAsync(world);

        await world.RegisterHandler().Handle(
            new RegisterDocumentCommand(draft.DocumentId, world.User.UserId, NumberingYear),
            CancellationToken.None);

        var result = await world.RegisterHandler().Handle(
            new RegisterDocumentCommand(draft.DocumentId, world.User.UserId, NumberingYear),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.OnlyDraftRegisterable.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Single(world.Numbers.DocumentNumbers);
    }

    [Fact]
    public async Task UpdateMetadata_WhenDocumentIsRegistered_ReturnsConflict()
    {
        var world = new World();
        var draft = await CreateDraftAsync(world);

        await world.RegisterHandler().Handle(
            new RegisterDocumentCommand(draft.DocumentId, world.User.UserId, NumberingYear),
            CancellationToken.None);

        var result = await world.UpdateHandler().Handle(
            new UpdateDraftMetadataCommand(draft.DocumentId, "عنوان جديد"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.OnlyDraftEditable.Code, result.Error.Code);
    }

    [Fact]
    public async Task UpdateMetadata_WhenDocumentIsDraft_ChangesFields()
    {
        var world = new World();
        var draft = await CreateDraftAsync(world);

        var result = await world.UpdateHandler().Handle(
            new UpdateDraftMetadataCommand(
                draft.DocumentId,
                "عنوان معدل",
                "ملخص معدل",
                DocumentConfidentialityLevel.SecretId,
                DocumentPriorityLevel.UrgentId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("عنوان معدل", result.Value.Title);
        Assert.Equal(DocumentConfidentialityLevel.SecretId, result.Value.ConfidentialityLevelId);
        Assert.Equal(DocumentPriorityLevel.UrgentId, result.Value.PriorityLevelId);
    }

    [Fact]
    public async Task AddParticipant_WithPerson_AttachesParticipant()
    {
        var world = new World();
        var draft = await CreateDraftAsync(world);

        var result = await world.AddParticipantHandler().Handle(
            new AddDocumentParticipantCommand(
                draft.DocumentId,
                ParticipantRole.SenderId,
                PersonName: "أحمد"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(ParticipantTargetType.PersonId, result.Value.TargetTypeId);
        Assert.Equal("أحمد", result.Value.PersonName);
        Assert.Single(world.Documents.Documents.Single().Participants);
    }

    [Fact]
    public async Task AddParticipant_WithTwoTargets_ReturnsValidationError()
    {
        var world = new World();
        var draft = await CreateDraftAsync(world);

        var result = await world.AddParticipantHandler().Handle(
            new AddDocumentParticipantCommand(
                draft.DocumentId,
                ParticipantRole.SenderId,
                InternalDepartmentId: Guid.NewGuid(),
                PersonName: "أحمد"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.ParticipantTargetInvalid.Code, result.Error.Code);
    }

    [Fact]
    public async Task AddParticipant_WithSecondExternalOrganization_ReturnsConflict()
    {
        var world = new World();
        var draft = await CreateDraftAsync(world);

        var first = ExternalOrganization.Create("MOI", "وزارة الداخلية");
        var second = ExternalOrganization.Create("MOE", "وزارة التربية");
        Assert.True(first.IsSuccess && second.IsSuccess);
        world.ExternalOrganizations.Add(first.Value);
        world.ExternalOrganizations.Add(second.Value);

        var handler = world.AddParticipantHandler();

        var firstResult = await handler.Handle(new AddDocumentParticipantCommand(
            draft.DocumentId,
            ParticipantRole.SenderId,
            ExternalOrganizationId: first.Value.ExternalOrganizationId), CancellationToken.None);
        Assert.True(firstResult.IsSuccess);

        var secondResult = await handler.Handle(new AddDocumentParticipantCommand(
            draft.DocumentId,
            ParticipantRole.SenderId,
            ExternalOrganizationId: second.Value.ExternalOrganizationId), CancellationToken.None);

        Assert.True(secondResult.IsFailure);
        Assert.Equal(
            DomainErrors.Correspondence.SingleExternalPartyLimit.Code, secondResult.Error.Code);
        Assert.Equal(ErrorType.Conflict, secondResult.Error.Type);
    }

    [Fact]
    public async Task Cancel_WhenDocumentHasActiveMovements_ReturnsConflict()
    {
        var world = new World();
        var draft = await CreateDraftAsync(world);
        world.Movements.DocumentIdsWithActiveMovements.Add(draft.DocumentId);

        var result = await world.CancelHandler().Handle(
            new CancelDocumentCommand(draft.DocumentId, world.User.UserId, "خطأ في الإدخال"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.HasActiveMovements.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Cancel_WhenDocumentIsDraft_CancelsWithReason()
    {
        var world = new World();
        var draft = await CreateDraftAsync(world);

        var result = await world.CancelHandler().Handle(
            new CancelDocumentCommand(draft.DocumentId, world.User.UserId, "خطأ في الإدخال"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(DocumentStatusType.CanceledId, result.Value.StatusId);

        var document = world.Documents.Documents.Single();
        Assert.Equal("خطأ في الإدخال", document.StatusHistory.Last().Reason);
    }

    [Fact]
    public async Task GetDetails_ReturnsParticipantsAndStatusHistory()
    {
        var world = new World();
        var draft = await CreateDraftAsync(world);

        await world.AddParticipantHandler().Handle(
            new AddDocumentParticipantCommand(
                draft.DocumentId, ParticipantRole.SenderId, PersonName: "أحمد"),
            CancellationToken.None);

        var result = await new GetDocumentDetailsQueryHandler(world.Documents)
            .Handle(new GetDocumentDetailsQuery(draft.DocumentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value.Participants);
        Assert.Single(result.Value.StatusHistory);
        Assert.Equal(DocumentStatusType.DraftId, result.Value.StatusHistory.Single().ToStatusId);
    }

    [Fact]
    public async Task GetTimeline_IncludesStatusParticipantAndNumberEvents()
    {
        var world = new World();
        var draft = await CreateDraftAsync(world);

        await world.AddParticipantHandler().Handle(
            new AddDocumentParticipantCommand(
                draft.DocumentId, ParticipantRole.SenderId, PersonName: "أحمد"),
            CancellationToken.None);

        await world.RegisterHandler().Handle(
            new RegisterDocumentCommand(draft.DocumentId, world.User.UserId, NumberingYear),
            CancellationToken.None);

        var result = await new GetDocumentTimelineQueryHandler(world.Documents, world.Numbers)
            .Handle(new GetDocumentTimelineQuery(draft.DocumentId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value, entry => entry.EventKind == "STATUS");
        Assert.Contains(result.Value, entry => entry.EventKind == "PARTICIPANT");
        Assert.Contains(result.Value, entry => entry.EventKind == "NUMBER");
    }
}
