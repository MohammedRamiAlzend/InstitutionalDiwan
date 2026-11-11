using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Application.Features.Persons;
using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DocumentFieldHandlerTests
{
    private const int TypeId = DocumentType.DecisionId;
    private static readonly Guid Admin = Guid.NewGuid();

    private sealed class World
    {
        public FakeDocumentTypeRepository Types { get; } = FakeDocumentTypeRepository.WithDefaultTypes();
        public FakeDocumentFieldRepository Fields { get; } = new();
        public FakeDocumentTypeSchemaVersionRepository Versions { get; } = new();
        public FakeDocumentRepository Documents { get; } = new();
        public FakeUserRepository Users { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public User Author { get; }

        public World()
        {
            var user = User.Create("author", "hash", Guid.NewGuid(), mustChangePassword: false);
            Assert.True(user.IsSuccess);
            Author = user.Value;
            Users.Add(Author);
        }

        public async Task<DocumentFieldResponse> CreateFieldAsync(string key, DocumentFieldDataType type)
        {
            var result = await new CreateDocumentFieldCommandHandler(Types, Fields, new FakeDepartmentRepository(), UnitOfWork)
                .Handle(new CreateDocumentFieldCommand(Admin, TypeId, key, type), CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
            return result.Value;
        }

        public async Task<SchemaVersionResponse> CreateDraftAsync()
        {
            var result = await new CreateSchemaDraftCommandHandler(Types, Versions, new FakeDepartmentRepository(), UnitOfWork)
                .Handle(new CreateSchemaDraftCommand(Admin, TypeId), CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
            return result.Value;
        }

        public async Task<SchemaVersionResponse> AddFieldAsync(
            Guid versionId, Guid fieldId, DocumentFieldSettingsInput settings)
        {
            var result = await new AddFieldToDraftCommandHandler(Versions, Fields, new FakeDocumentFieldOptionResolver(), UnitOfWork)
                .Handle(new AddFieldToDraftCommand(versionId, fieldId, settings), CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
            return result.Value;
        }

        public async Task<SchemaVersionResponse> PublishAsync(Guid versionId)
        {
            var result = await new PublishSchemaVersionCommandHandler(Versions, UnitOfWork)
                .Handle(new PublishSchemaVersionCommand(versionId, Admin), CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
            return result.Value;
        }

        /// <summary>Publishes a schema with a required text field "session_no" and an optional number "amount".</summary>
        public async Task<SchemaVersionResponse> PublishBasicSchemaAsync()
        {
            var session = await CreateFieldAsync("session_no", DocumentFieldDataType.Text);
            var amount = await CreateFieldAsync("amount", DocumentFieldDataType.Number);
            var draft = await CreateDraftAsync();
            await AddFieldAsync(draft.SchemaVersionId, session.DocumentFieldId,
                new DocumentFieldSettingsInput("رقم الجلسة", IsRequired: true, DisplayOrder: 1,
                    RegexPattern: "^[0-9]{1,4}$", RegexErrorMessageAr: "رقم الجلسة أرقام فقط"));
            await AddFieldAsync(draft.SchemaVersionId, amount.DocumentFieldId,
                new DocumentFieldSettingsInput("المبلغ", DisplayOrder: 2, MinNumber: 0));
            return await PublishAsync(draft.SchemaVersionId);
        }

        public CreateDraftDocumentCommandHandler DraftHandler() =>
            new(Users, Documents, Types, Versions, new FakeDocumentFieldOptionResolver(), UnitOfWork, new DocumentPersonLinker(new FakePersonRepository(), new FakeLookupRepository()), new FakeInstitutionSettingRepository());

        public async Task<DocumentResponse> CreateDraftDocumentAsync(params DocumentFieldValueInput[] values)
        {
            var result = await DraftHandler().Handle(
                new CreateDraftDocumentCommand(Author.UserId, "قرار", TypeId, DirectionId: DocumentDirection.InternalId,
                    FieldValues: values),
                CancellationToken.None);
            Assert.True(result.IsSuccess, result.IsFailure ? result.Error.Code : null);
            return result.Value;
        }
    }

    [Fact]
    public async Task CreateField_RejectsDuplicateKeyAndUnknownType()
    {
        var world = new World();
        await world.CreateFieldAsync("session_no", DocumentFieldDataType.Text);
        var handler = new CreateDocumentFieldCommandHandler(world.Types, world.Fields, new FakeDepartmentRepository(), world.UnitOfWork);

        var duplicate = await handler.Handle(
            new CreateDocumentFieldCommand(Admin, TypeId, "session_no", DocumentFieldDataType.Number), CancellationToken.None);
        var unknownType = await handler.Handle(
            new CreateDocumentFieldCommand(Admin, 999, "x", DocumentFieldDataType.Text), CancellationToken.None);

        Assert.Equal("DocumentFields.KeyAlreadyExists", duplicate.Error.Code);
        Assert.Equal("Correspondence.DocumentTypeNotFound", unknownType.Error.Code);
    }

    [Fact]
    public async Task SchemaLifecycle_PublishRetiresThePreviousVersion_AndNextDraftCopiesIt()
    {
        var world = new World();
        var v1 = await world.PublishBasicSchemaAsync();

        Assert.Equal(1, v1.VersionNumber);
        Assert.Equal(DocumentTypeSchemaVersionStatus.Published, v1.Status);
        Assert.Equal(new[] { "session_no", "amount" }, v1.Fields.Select(f => f.Key));

        var draft = await world.CreateDraftAsync();
        Assert.Equal(2, draft.VersionNumber);
        Assert.Equal(2, draft.Fields.Count);

        var again = await new CreateSchemaDraftCommandHandler(world.Types, world.Versions, new FakeDepartmentRepository(), world.UnitOfWork)
            .Handle(new CreateSchemaDraftCommand(Admin, TypeId), CancellationToken.None);
        Assert.Equal("DocumentFields.SchemaVersionAlreadyHasDraft", again.Error.Code);

        await world.PublishAsync(draft.SchemaVersionId);

        var statuses = world.Versions.Versions.OrderBy(v => v.VersionNumber).Select(v => v.Status).ToList();
        Assert.Equal(new[] { DocumentTypeSchemaVersionStatus.Retired, DocumentTypeSchemaVersionStatus.Published }, statuses);
    }

    [Fact]
    public async Task Discard_OnlyRemovesADraft()
    {
        var world = new World();
        var published = await world.PublishBasicSchemaAsync();
        var draft = await world.CreateDraftAsync();
        var handler = new DiscardSchemaDraftCommandHandler(world.Versions, world.UnitOfWork);

        var onPublished = await handler.Handle(new DiscardSchemaDraftCommand(published.SchemaVersionId), CancellationToken.None);
        var onDraft = await handler.Handle(new DiscardSchemaDraftCommand(draft.SchemaVersionId), CancellationToken.None);

        Assert.Equal("DocumentFields.SchemaVersionNotDraft", onPublished.Error.Code);
        Assert.True(onDraft.IsSuccess);
        Assert.Single(world.Versions.Versions);
    }

    [Fact]
    public async Task AddAndUpdateField_ValidateSettingsAgainstTheFieldType_AndPublishedIsFrozen()
    {
        var world = new World();
        var amount = await world.CreateFieldAsync("amount", DocumentFieldDataType.Number);
        var draft = await world.CreateDraftAsync();
        var add = new AddFieldToDraftCommandHandler(world.Versions, world.Fields, new FakeDocumentFieldOptionResolver(), world.UnitOfWork);

        var badRegex = await add.Handle(
            new AddFieldToDraftCommand(draft.SchemaVersionId, amount.DocumentFieldId,
                new DocumentFieldSettingsInput("المبلغ", RegexPattern: "^a$", RegexErrorMessageAr: "خطأ")),
            CancellationToken.None);
        Assert.Equal("DocumentFields.RegexNotAllowed", badRegex.Error.Code);

        await world.AddFieldAsync(draft.SchemaVersionId, amount.DocumentFieldId, new DocumentFieldSettingsInput("المبلغ"));
        var update = new UpdateDraftFieldCommandHandler(world.Versions, new FakeDocumentFieldOptionResolver(), world.UnitOfWork);
        var updated = await update.Handle(
            new UpdateDraftFieldCommand(draft.SchemaVersionId, amount.DocumentFieldId,
                new DocumentFieldSettingsInput("المبلغ الكلي", IsRequired: true, MinNumber: 1, MaxNumber: 5)),
            CancellationToken.None);
        Assert.True(updated.IsSuccess);
        Assert.Equal("المبلغ الكلي", Assert.Single(updated.Value.Fields).LabelAr);

        await world.PublishAsync(draft.SchemaVersionId);
        var afterPublish = await update.Handle(
            new UpdateDraftFieldCommand(draft.SchemaVersionId, amount.DocumentFieldId, new DocumentFieldSettingsInput("آخر")),
            CancellationToken.None);
        Assert.Equal("DocumentFields.SchemaVersionNotDraft", afterPublish.Error.Code);
    }

    [Fact]
    public async Task NewDocument_IsPinnedToThePublishedSchema_AndKeepsItWhenANewVersionIsPublished()
    {
        var world = new World();
        var v1 = await world.PublishBasicSchemaAsync();

        var created = await world.CreateDraftDocumentAsync(
            new DocumentFieldValueInput("session_no", "12"), new DocumentFieldValueInput("amount", "7.5"));
        var document = world.Documents.Documents.Single(d => d.DocumentId == created.DocumentId);
        Assert.Equal(v1.SchemaVersionId, document.SchemaVersionId);
        Assert.Equal(2, document.FieldValues.Count);

        // Publish a new version that drops "amount"; the existing document must not move.
        var draft = await world.CreateDraftAsync();
        await new RemoveDraftFieldCommandHandler(world.Versions, world.UnitOfWork).Handle(
            new RemoveDraftFieldCommand(draft.SchemaVersionId, draft.Fields.Single(f => f.Key == "amount").DocumentFieldId),
            CancellationToken.None);
        var v2 = await world.PublishAsync(draft.SchemaVersionId);

        Assert.Equal(v1.SchemaVersionId, document.SchemaVersionId);
        var access = new FakeDocumentAccessEvaluator();
        access.ViewableDocuments.Add(document.DocumentId);
        var fields = await new GetDocumentFieldsQueryHandler(world.Documents, world.Versions, access, new FakeDocumentFieldOptionResolver())
            .Handle(new GetDocumentFieldsQuery(document.DocumentId), CancellationToken.None);
        Assert.Equal(1, fields.Value.VersionNumber);
        Assert.Equal("12", fields.Value.Fields.Single(f => f.Field.Key == "session_no").Value);
        Assert.Equal("7.5", fields.Value.Fields.Single(f => f.Field.Key == "amount").Value);

        var another = await world.CreateDraftDocumentAsync(new DocumentFieldValueInput("session_no", "3"));
        Assert.Equal(v2.SchemaVersionId, world.Documents.Documents.Single(d => d.DocumentId == another.DocumentId).SchemaVersionId);
    }

    [Fact]
    public async Task CreateDocument_RejectsValuesThatBreakTheSchema_AndValuesWithoutASchema()
    {
        var world = new World();
        var noSchema = await world.DraftHandler().Handle(
            new CreateDraftDocumentCommand(world.Author.UserId, "قرار", TypeId, DirectionId: DocumentDirection.InternalId,
                FieldValues: new[] { new DocumentFieldValueInput("session_no", "1") }),
            CancellationToken.None);
        Assert.Equal("DocumentFields.UnknownField", noSchema.Error.Code);

        await world.PublishBasicSchemaAsync();
        var badPattern = await world.DraftHandler().Handle(
            new CreateDraftDocumentCommand(world.Author.UserId, "قرار", TypeId, DirectionId: DocumentDirection.InternalId,
                FieldValues: new[] { new DocumentFieldValueInput("session_no", "abc") }),
            CancellationToken.None);
        Assert.Equal("DocumentFields.ValuePatternMismatch", badPattern.Error.Code);
        Assert.Empty(world.Documents.Documents);
    }

    [Fact]
    public async Task SetValues_ReplacesDraftValues_AndIsLockedAfterRegistration()
    {
        var world = new World();
        await world.PublishBasicSchemaAsync();
        var created = await world.CreateDraftDocumentAsync(new DocumentFieldValueInput("session_no", "1"));
        var handler = new SetDocumentFieldValuesCommandHandler(
            world.Documents, world.Versions, new FakeDocumentAccessEvaluator { AllowAll = true }, new FakeDocumentFieldOptionResolver(), world.UnitOfWork);

        var set = await handler.Handle(
            new SetDocumentFieldValuesCommand(created.DocumentId, new[]
            {
                new DocumentFieldValueInput("session_no", "9"), new DocumentFieldValueInput("amount", "")
            }), CancellationToken.None);
        Assert.True(set.IsSuccess);
        Assert.Equal("9", set.Value.Fields.Single(f => f.Field.Key == "session_no").Value);
        Assert.Null(set.Value.Fields.Single(f => f.Field.Key == "amount").Value);

        var document = world.Documents.Documents.Single();
        document.Register(Guid.NewGuid(), OfficialNumberValue.Create("1/2026").Value, world.Author.UserId);
        var locked = await handler.Handle(
            new SetDocumentFieldValuesCommand(created.DocumentId, new[] { new DocumentFieldValueInput("session_no", "5") }),
            CancellationToken.None);
        Assert.Equal("Correspondence.OnlyDraftEditable", locked.Error.Code);
    }

    [Fact]
    public async Task Register_IsBlockedByAMissingRequiredField_BeforeANumberIsAllocated()
    {
        var world = new World();
        await world.PublishBasicSchemaAsync();
        var created = await world.CreateDraftDocumentAsync(new DocumentFieldValueInput("amount", "2"));
        var numbers = new FakeDocumentNumberRepository();
        var handler = new RegisterDocumentCommandHandler(
            world.Documents, numbers, new FakeNumberingRegistryRepository(), world.Versions,
            new FakeDocumentAccessEvaluator { AllowAll = true }, world.UnitOfWork, FakeNotificationPublisher.Instance, new FakeSender());

        var blocked = await handler.Handle(
            new RegisterDocumentCommand(created.DocumentId, world.Author.UserId, 2026), CancellationToken.None);

        Assert.Equal("DocumentFields.ValueRequired", blocked.Error.Code);
        Assert.Empty(numbers.DocumentNumbers);
        Assert.Equal(Document.StatusDraft, world.Documents.Documents.Single().CurrentStatusId);
    }

    [Fact]
    public async Task CreateAndRegister_RequiresTheRequiredFields_ThenRegistersWithTheirValues()
    {
        var world = new World();
        await world.PublishBasicSchemaAsync();
        var numbers = new FakeDocumentNumberRepository();
        var registries = new FakeNumberingRegistryRepository();
        registries.Add(NumberingRegistry.Create("DEPT-BOOK", "دفتر القسم", departmentId: world.Author.PrimaryDepartmentId).Value);
        var handler = new CreateDocumentCommandHandler(
            world.Users, world.Documents, world.Types, numbers, registries,
            world.Versions, new FakeDocumentFieldOptionResolver(), world.UnitOfWork, new FakeSender(),
            new DocumentPersonLinker(new FakePersonRepository(), new FakeLookupRepository()), new FakeInstitutionSettingRepository());

        var missing = await handler.Handle(
            new CreateDocumentCommand(world.Author.UserId, "قرار", TypeId, "9/2026", new DateTime(2026, 3, 9), DirectionId: DocumentDirection.InternalId),
            CancellationToken.None);
        Assert.Equal("DocumentFields.ValueRequired", missing.Error.Code);
        Assert.Empty(world.Documents.Documents);
        Assert.Empty(numbers.DocumentNumbers);

        var created = await handler.Handle(
            new CreateDocumentCommand(world.Author.UserId, "قرار", TypeId, "9/2026", new DateTime(2026, 3, 9), DirectionId: DocumentDirection.InternalId,
                FieldValues: new[] { new DocumentFieldValueInput("session_no", "4") }),
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.IsFailure ? created.Error.Code : null);
        var document = Assert.Single(world.Documents.Documents);
        Assert.Equal(Document.StatusRegistered, document.CurrentStatusId);
        Assert.NotNull(document.SchemaVersionId);
        Assert.Single(document.FieldValues);
    }

    [Fact]
    public async Task Queries_ReturnTheTypeSchema_TheHistoryAndTheFieldList()
    {
        var world = new World();
        var empty = await new GetDocumentTypeSchemaQueryHandler(world.Types, world.Versions, new FakeDocumentFieldOptionResolver())
            .Handle(new GetDocumentTypeSchemaQuery(TypeId), CancellationToken.None);
        Assert.Null(empty.Value.Schema);

        var v1 = await world.PublishBasicSchemaAsync();
        await world.CreateDraftAsync();

        var schema = await new GetDocumentTypeSchemaQueryHandler(world.Types, world.Versions, new FakeDocumentFieldOptionResolver())
            .Handle(new GetDocumentTypeSchemaQuery(TypeId), CancellationToken.None);
        Assert.Equal(v1.SchemaVersionId, schema.Value.Schema!.SchemaVersionId);

        var history = await new ListSchemaVersionsQueryHandler(world.Types, world.Versions)
            .Handle(new ListSchemaVersionsQuery(TypeId), CancellationToken.None);
        Assert.Equal(new[] { 2, 1 }, history.Value.Select(v => v.VersionNumber));
        Assert.Equal(DocumentTypeSchemaVersionStatus.Draft, history.Value[0].Status);

        var fields = await new ListDocumentFieldsQueryHandler(world.Types, world.Fields)
            .Handle(new ListDocumentFieldsQuery(TypeId), CancellationToken.None);
        Assert.Equal(2, fields.Value.Count);

        var missing = await new GetSchemaVersionQueryHandler(world.Versions, new FakeDocumentFieldOptionResolver())
            .Handle(new GetSchemaVersionQuery(Guid.NewGuid()), CancellationToken.None);
        Assert.Equal("DocumentFields.SchemaVersionNotFound", missing.Error.Code);
    }

    [Fact]
    public async Task NewRequiredField_InANewVersion_DoesNotBlockRegisteringADocumentOnTheOldVersion()
    {
        var world = new World();
        await world.PublishBasicSchemaAsync();
        var created = await world.CreateDraftDocumentAsync(new DocumentFieldValueInput("session_no", "5"));

        var extra = await world.CreateFieldAsync("reviewer", DocumentFieldDataType.Text);
        var draft = await world.CreateDraftAsync();
        await world.AddFieldAsync(draft.SchemaVersionId, extra.DocumentFieldId,
            new DocumentFieldSettingsInput("المراجع", IsRequired: true));
        await world.PublishAsync(draft.SchemaVersionId);

        var registered = await new RegisterDocumentCommandHandler(
            world.Documents, new FakeDocumentNumberRepository(), new FakeNumberingRegistryRepository(),
            world.Versions, new FakeDocumentAccessEvaluator { AllowAll = true }, world.UnitOfWork, FakeNotificationPublisher.Instance, new FakeSender()).Handle(
            new RegisterDocumentCommand(created.DocumentId, world.Author.UserId, 2026), CancellationToken.None);

        Assert.True(registered.IsSuccess, registered.IsFailure ? registered.Error.Code : null);

        var newDocument = await world.CreateDraftDocumentAsync(new DocumentFieldValueInput("session_no", "6"));
        var blocked = await new RegisterDocumentCommandHandler(
            world.Documents, new FakeDocumentNumberRepository(), new FakeNumberingRegistryRepository(),
            world.Versions, new FakeDocumentAccessEvaluator { AllowAll = true }, world.UnitOfWork, FakeNotificationPublisher.Instance, new FakeSender()).Handle(
            new RegisterDocumentCommand(newDocument.DocumentId, world.Author.UserId, 2026), CancellationToken.None);
        Assert.Equal("DocumentFields.ValueRequired", blocked.Error.Code);
    }

    [Fact]
    public void Validators_RejectMalformedCommands()
    {
        var settings = new DocumentFieldSettingsInputValidator();
        Assert.False(settings.Validate(new DocumentFieldSettingsInput(" ")).IsValid);
        Assert.False(settings.Validate(new DocumentFieldSettingsInput(new string('x', 201))).IsValid);
        Assert.True(settings.Validate(new DocumentFieldSettingsInput("رقم الجلسة")).IsValid);

        var create = new CreateDocumentFieldCommandValidator();
        Assert.False(create.Validate(new CreateDocumentFieldCommand(Admin, TypeId, "Bad Key", DocumentFieldDataType.Text)).IsValid);
        Assert.False(create.Validate(new CreateDocumentFieldCommand(Admin, TypeId, "ok_key", (DocumentFieldDataType)99)).IsValid);
        Assert.False(create.Validate(new CreateDocumentFieldCommand(Guid.Empty, 0, "ok_key", DocumentFieldDataType.Text)).IsValid);
        Assert.True(create.Validate(new CreateDocumentFieldCommand(Admin, TypeId, "ok_key", DocumentFieldDataType.Text)).IsValid);

        var values = new SetDocumentFieldValuesCommandValidator();
        Assert.False(values.Validate(new SetDocumentFieldValuesCommand(Guid.NewGuid(),
            new[] { new DocumentFieldValueInput(" ", "1") })).IsValid);
        Assert.True(values.Validate(new SetDocumentFieldValuesCommand(Guid.NewGuid(),
            new[] { new DocumentFieldValueInput("session_no", "1") })).IsValid);
    }
}
