using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Infrastructure.Authorization;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>
/// Editing a draft, its field values, registering it and adding a participant require DOWNLOAD, so a
/// department keeps working on its own documents; cancelling requires MANAGE. A denied write changes nothing.
/// </summary>
public sealed class DocumentWriteAccessHandlerTests
{
    private sealed class World
    {
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentNumberRepository Numbers { get; } = new();
        public FakeNumberingRegistryRepository Registries { get; } = new();
        public FakeDocumentMovementRepository Movements { get; } = new();
        public FakeLookupRepository Lookups { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public FakeDocumentAccessEvaluator Access { get; } = new();
        public Guid ActorId { get; } = Guid.NewGuid();
        public Document Document { get; }

        public World()
        {
            Document = Document.CreateDraft(
                "كتاب",
                DocumentType.InternalMemoId,
                DocumentDirection.InternalId,
                Guid.NewGuid(),
                Guid.NewGuid()).Value;
            Documents.Add(Document);
            Registries.Add(NumberingRegistry.Create("DEFAULT", "السجل الافتراضي").Value);
            Lookups.Add("participant-roles", ParticipantRole.Create(ParticipantRole.SenderId, "Sender", "مرسل").Value);
        }

        public async Task<Result> UpdateAsync(Guid? documentId = null) =>
            await new UpdateDraftMetadataCommandHandler(Documents, Access, UnitOfWork).Handle(
                new UpdateDraftMetadataCommand(documentId ?? Document.DocumentId, "عنوان جديد", "ملخص"),
                CancellationToken.None);

        public async Task<Result> SetFieldsAsync(Guid? documentId = null) =>
            await new SetDocumentFieldValuesCommandHandler(
                Documents, new FakeDocumentTypeSchemaVersionRepository(), Access, UnitOfWork).Handle(
                new SetDocumentFieldValuesCommand(documentId ?? Document.DocumentId, Array.Empty<DocumentFieldValueInput>()),
                CancellationToken.None);

        public async Task<Result> RegisterAsync(Guid? documentId = null) =>
            await new RegisterDocumentCommandHandler(
                Documents, Numbers, Registries, new FakeDocumentTypeSchemaVersionRepository(), Access, UnitOfWork, TestClock.Instance).Handle(
                new RegisterDocumentCommand(documentId ?? Document.DocumentId, ActorId, 2026),
                CancellationToken.None);

        public async Task<Result> AddParticipantAsync(Guid? documentId = null) =>
            await new AddDocumentParticipantCommandHandler(
                Documents, new FakeDepartmentRepository(), new FakeExternalOrganizationRepository(), Lookups, Access, UnitOfWork).Handle(
                new AddDocumentParticipantCommand(documentId ?? Document.DocumentId, ParticipantRole.SenderId, PersonName: "أحمد"),
                CancellationToken.None);

        public async Task<Result> CancelAsync(Guid? documentId = null) =>
            await new CancelDocumentCommandHandler(Documents, Movements, Access, UnitOfWork).Handle(
                new CancelDocumentCommand(documentId ?? Document.DocumentId, ActorId, "سبب"),
                CancellationToken.None);
    }

    private static void AssertDenied(Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, result.Error.Code);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    private static void AssertNotFound(Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DocumentNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task Writes_WithViewOnlyAccess_AreForbiddenAndChangeNothing()
    {
        var world = new World();
        world.Access.ViewableDocuments.Add(world.Document.DocumentId);

        AssertDenied(await world.UpdateAsync());
        AssertDenied(await world.SetFieldsAsync());
        AssertDenied(await world.RegisterAsync());
        AssertDenied(await world.AddParticipantAsync());
        AssertDenied(await world.CancelAsync());

        Assert.Equal("كتاب", world.Document.Title);
        Assert.Equal(DocumentStatusType.DraftId, world.Document.CurrentStatusId);
        Assert.Null(world.Document.ActiveOfficialNumberId);
        Assert.Empty(world.Document.Participants);
        Assert.Empty(await world.Numbers.ListByDocumentAsync(world.Document.DocumentId));
        Assert.Equal(0, world.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task DepartmentWrites_WithDownloadAccess_Succeed()
    {
        var world = new World();
        world.Access.DownloadableDocuments.Add(world.Document.DocumentId);

        Assert.True((await world.UpdateAsync()).IsSuccess);
        Assert.True((await world.SetFieldsAsync()).IsSuccess);
        Assert.True((await world.AddParticipantAsync()).IsSuccess);
        Assert.True((await world.RegisterAsync()).IsSuccess);
        Assert.Equal(DocumentStatusType.RegisteredId, world.Document.CurrentStatusId);
    }

    [Fact]
    public async Task Cancel_WithDownloadAccess_IsForbidden()
    {
        var world = new World();
        world.Access.DownloadableDocuments.Add(world.Document.DocumentId);

        AssertDenied(await world.CancelAsync());
        Assert.Equal(DocumentStatusType.DraftId, world.Document.CurrentStatusId);
    }

    [Fact]
    public async Task Cancel_WithManageAccess_Succeeds()
    {
        var world = new World();
        world.Access.ManagedDocuments.Add(world.Document.DocumentId);

        Assert.True((await world.CancelAsync()).IsSuccess);
        Assert.Equal(DocumentStatusType.CanceledId, world.Document.CurrentStatusId);
    }

    [Fact]
    public async Task Writes_OnAMissingDocument_AreNotFound()
    {
        var world = new World();
        var id = Guid.NewGuid();

        AssertNotFound(await world.UpdateAsync(id));
        AssertNotFound(await world.SetFieldsAsync(id));
        AssertNotFound(await world.RegisterAsync(id));
        AssertNotFound(await world.AddParticipantAsync(id));
        AssertNotFound(await world.CancelAsync(id));
    }

    /// <summary>
    /// The chosen levels against the real evaluator: who ends up able to edit (DOWNLOAD) and cancel (MANAGE).
    /// </summary>
    [Theory]
    [InlineData("author", DocumentConfidentialityLevel.NormalId, true, true)]
    [InlineData("author", DocumentConfidentialityLevel.SecretId, true, true)]
    [InlineData("colleague", DocumentConfidentialityLevel.NormalId, true, false)]
    [InlineData("colleague", DocumentConfidentialityLevel.SecretId, false, false)]
    [InlineData("colleague", DocumentConfidentialityLevel.TopSecretId, false, false)]
    [InlineData("outsider", DocumentConfidentialityLevel.NormalId, false, false)]
    [InlineData("institution", DocumentConfidentialityLevel.NormalId, true, true)]
    [InlineData("institution", DocumentConfidentialityLevel.SecretId, false, false)]
    public async Task ChosenLevels_AgainstTheEvaluator(string caller, int confidentiality, bool canEdit, bool canCancel)
    {
        var owningDepartmentId = Guid.NewGuid();
        var context = new FakeUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() };
        var scope = new FakeDepartmentScopeEvaluator();
        if (caller == "colleague")
            scope.AccessibleDepartments.Add(owningDepartmentId);
        if (caller == "institution")
        {
            scope.InstitutionWide = true;
            context.ScopeTypeId = UserScopeType.InstitutionId;
        }

        var document = Document.CreateDraft(
            "كتاب",
            DocumentType.InternalMemoId,
            DocumentDirection.InternalId,
            owningDepartmentId,
            caller == "author" ? context.UserId : Guid.NewGuid(),
            confidentialityLevelId: confidentiality).Value;
        var documents = new FakeDocumentRepository();
        documents.Add(document);

        var evaluator = new DocumentAccessEvaluator(
            context, documents, new FakeDocumentAccessGrantRepository(), scope, new FakeDocumentMovementRepository(), TestClock.Instance);

        Assert.Equal(canEdit, await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.DownloadId));
        Assert.Equal(canCancel, await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ManageId));
    }
}
