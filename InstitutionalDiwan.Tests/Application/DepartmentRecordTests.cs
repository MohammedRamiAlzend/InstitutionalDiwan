using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Application.Features.DepartmentRecords;
using InstitutionalDiwan.Application.Features.Movements;
using InstitutionalDiwan.Application.Features.Numbering.Numbers;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>
/// The record each department keeps on its visits of a document: opened at receipt with the
/// department's own form, locked when the document leaves, corrected only with a reason, and shown
/// only to the department itself or to holders of DEPARTMENT_RECORD_VIEW_ALL.
/// </summary>
public sealed class DepartmentRecordTests
{
    private static readonly Guid Admin = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly DateTime Today = DateTime.UtcNow.Date;

    private sealed class World
    {
        public FakeUserRepository Users { get; } = new();
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentNumberRepository Numbers { get; } = new();
        public FakeNumberingRegistryRepository Registries { get; } = new();
        public FakeDepartmentRepository Departments { get; } = new();
        public FakeInstitutionSettingRepository Settings { get; } = new();
        public FakeDocumentMovementRepository Movements { get; } = new();
        public FakeDepartmentScopeEvaluator CustodyScope { get; } = new() { InstitutionWide = true };
        public FakeDocumentTypeSchemaVersionRepository RecordForms { get; } = new();
        public FakeDocumentFieldOptionResolver Options { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();

        public User Clerk { get; }
        public User Investigator { get; }
        public Guid DiwanId { get; }
        public Guid InvestigationId { get; }
        public Guid HrId { get; }
        public Document Document { get; }

        public World()
        {
            DiwanId = AddDepartment("DIWAN");
            InvestigationId = AddDepartment("INV");
            HrId = AddDepartment("HR");

            Settings.Add(InstitutionSetting.Create("المؤسسة", 1, 1, centralDiwanDepartmentId: DiwanId).Value);

            Clerk = User.Create("clerk", "hash", DiwanId).Value;
            Investigator = User.Create("investigator", "hash", InvestigationId).Value;
            Users.Add(Clerk);
            Users.Add(Investigator);

            Document = Document.CreateDraft(
                "طلب تحقيق", DocumentType.InternalMemoId, DocumentDirection.InternalId, DiwanId, Clerk.UserId).Value;
            Documents.Add(Document);
            Registries.Add(NumberingRegistry.Create("INV", "سجل التحقيق", departmentId: InvestigationId).Value);
        }

        private Guid AddDepartment(string code)
        {
            var department = Department.Create(code, $"ديوان {code}", 1).Value;
            Departments.Add(department);
            return department.DepartmentId;
        }

        public DepartmentRecordService Records() => FakeDepartmentRecords.Service(Documents, RecordForms, Options);

        public CentralDiwanRouting Routing() => new(Settings);

        public async Task RegisterAsync()
        {
            var result = await new RegisterDocumentCommandHandler(
                    Documents, Numbers, Registries, new FakeDocumentTypeSchemaVersionRepository(),
                    new FakeDocumentAccessEvaluator { AllowAll = true }, UnitOfWork, FakeNotificationPublisher.Instance, new FakeSender())
                .Handle(new RegisterDocumentCommand(Document.DocumentId, Clerk.UserId, Today.Year), CancellationToken.None);
            Assert.True(result.IsSuccess, result.Error?.Code);
        }

        /// <summary>Writes a register line (and its first visit) for a department, as the clerk would.</summary>
        public DocumentNumber AddLine(Guid departmentId, string number)
        {
            var line = DocumentNumber.CreateDepartmentEntry(
                Document.DocumentId, departmentId, Guid.NewGuid(), number, Today, Clerk.UserId).Value;
            Numbers.Add(line);
            Assert.True(Document.AddDepartmentNumber(line).IsSuccess);
            return line;
        }

        /// <summary>Publishes the investigation department's record form: a required case number and an optional note.</summary>
        public DocumentTypeSchemaVersion PublishInvestigationForm()
        {
            var caseNo = DocumentField.Create(DocumentType.InternalMemoId, "case_no", DocumentFieldDataType.Text, Admin, departmentId: InvestigationId).Value;
            var note = DocumentField.Create(DocumentType.InternalMemoId, "note", DocumentFieldDataType.Text, Admin, departmentId: InvestigationId).Value;
            var form = DocumentTypeSchemaVersion.CreateDraft(DocumentType.InternalMemoId, 1, Admin, departmentId: InvestigationId).Value;
            Assert.True(form.AddField(caseNo, DocumentFieldSettings.Create(DocumentFieldDataType.Text, "رقم القضية", true, 1).Value).IsSuccess);
            Assert.True(form.AddField(note, DocumentFieldSettings.Create(DocumentFieldDataType.Text, "ملاحظة", false, 2).Value).IsSuccess);
            Assert.True(form.Publish(Admin).IsSuccess);
            RecordForms.Add(form);
            return form;
        }

        public async Task<DocumentMovement> SendToInvestigationAsync()
        {
            var created = await new CreateMovementCommandHandler(
                    Movements, Documents, Departments, Users, Routing(), new FakeDocumentAccessEvaluator { AllowAll = true }, UnitOfWork)
                .Handle(new CreateMovementCommand(Document.DocumentId, Clerk.UserId, InvestigationId), CancellationToken.None);
            Assert.True(created.IsSuccess, created.Error?.Code);

            var sent = await new SendMovementCommandHandler(
                    Movements, Routing(), CustodyScope, UnitOfWork, FakeNotificationPublisher.Instance, Records())
                .Handle(new SendMovementCommand(created.Value.MovementId, Clerk.UserId), CancellationToken.None);
            Assert.True(sent.IsSuccess, sent.Error?.Code);
            return (await Movements.GetByIdAsync(created.Value.MovementId))!;
        }

        public Task<Result<MovementStepReceiptResponse>> ReceiveAsync(
            DocumentMovement movement, params (string Key, string Value)[] values) =>
            new ReceiveDocumentCommandHandler(
                    Movements, Documents, CustodyScope, UnitOfWork, FakeNotificationPublisher.Instance,
                    new DepartmentRegistrationService(Numbers, Departments, Registries, CustodyScope, UnitOfWork),
                    TestClock.Instance, Records())
                .Handle(new ReceiveDocumentCommand(
                    movement.MovementId, Investigator.UserId, RegistryDate: Today,
                    FieldValues: values.Select(value => new DocumentFieldValueInput(value.Key, value.Value)).ToList()),
                    CancellationToken.None);

        public SetVisitRecordCommandHandler SetHandler(FakeDepartmentScopeEvaluator scope) =>
            new(Numbers, Documents, RecordForms, Options, scope, new FakeUserContext(), UnitOfWork);

        public GetDocumentDepartmentRecordsQueryHandler QueryHandler(FakeDepartmentScopeEvaluator scope, params string[] permissions) =>
            new(Documents, Departments, RecordForms, Options, scope, new FakeDocumentAccessEvaluator { AllowAll = true },
                new FakeUserContext { Permissions = new HashSet<string>(permissions, StringComparer.OrdinalIgnoreCase) });

        public DocumentRegistryVisit Visit(Guid visitId) => Document.FindRegistryVisit(visitId)!.Value.Visit;
    }

    private static FakeDepartmentScopeEvaluator ScopeOf(Guid departmentId)
    {
        var scope = new FakeDepartmentScopeEvaluator();
        scope.AccessibleDepartments.Add(departmentId);
        return scope;
    }

    // ---- Domain ----

    [Fact]
    public void AddField_FromAnotherForm_IsRejected()
    {
        var department = Guid.NewGuid();
        var creationField = DocumentField.Create(DocumentType.DecisionId, "a", DocumentFieldDataType.Text, Admin).Value;
        var departmentForm = DocumentTypeSchemaVersion.CreateDraft(DocumentType.DecisionId, 1, Admin, departmentId: department).Value;
        var settings = DocumentFieldSettings.Create(DocumentFieldDataType.Text, "أ", false, 1).Value;

        Assert.Equal(DomainErrors.DocumentFields.FieldDocumentTypeMismatch.Code, departmentForm.AddField(creationField, settings).Error.Code);

        var draftFrom = DocumentTypeSchemaVersion.CreateDraftFrom(departmentForm, 2, Admin).Value;
        Assert.Equal(department, draftFrom.DepartmentId);
    }

    // ---- Receipt opens the record ----

    [Fact]
    public async Task Receive_WithAPublishedForm_MissingRequiredField_FailsTheWholeReceipt()
    {
        var world = new World();
        await world.RegisterAsync();
        world.PublishInvestigationForm();
        world.AddLine(world.InvestigationId, "INV-1");
        var movement = await world.SendToInvestigationAsync();
        var savesBefore = world.UnitOfWork.SaveChangesCallCount;

        var result = await world.ReceiveAsync(movement, ("note", "بلا رقم"));

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.DocumentFields.ValueRequired("case_no").Code, result.Error.Code);
        Assert.Equal(savesBefore, world.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Receive_WithAPublishedForm_OpensTheRecordPinnedToThatForm()
    {
        var world = new World();
        await world.RegisterAsync();
        var form = world.PublishInvestigationForm();
        world.AddLine(world.InvestigationId, "INV-1");
        var movement = await world.SendToInvestigationAsync();

        var result = await world.ReceiveAsync(movement, ("case_no", "77/2026"));

        Assert.True(result.IsSuccess, result.Error?.Code);
        var visit = world.Visit(result.Value.RegistryVisitId);
        Assert.Equal(form.SchemaVersionId, visit.SchemaVersionId);
        Assert.False(visit.IsLocked);
        Assert.Equal("77/2026", Assert.Single(visit.Values).TextValue);
    }

    [Fact]
    public async Task Receive_WithoutAForm_WorksAsBefore()
    {
        var world = new World();
        await world.RegisterAsync();
        world.AddLine(world.InvestigationId, "INV-1");
        var movement = await world.SendToInvestigationAsync();

        var plain = await world.ReceiveAsync(movement);

        Assert.True(plain.IsSuccess, plain.Error?.Code);
        Assert.Null(world.Visit(plain.Value.RegistryVisitId).SchemaVersionId);
    }

    [Fact]
    public async Task Receive_WithoutAForm_RejectsRecordValues()
    {
        var world = new World();
        await world.RegisterAsync();
        world.AddLine(world.InvestigationId, "INV-1");
        var movement = await world.SendToInvestigationAsync();

        var withValue = await world.ReceiveAsync(movement, ("case_no", "1"));

        Assert.Equal(DomainErrors.DocumentFields.UnknownField("case_no").Code, withValue.Error.Code);
    }

    // ---- Leaving locks the record ----

    [Fact]
    public async Task Send_LocksTheSendersOpenRecord()
    {
        var world = new World();
        await world.RegisterAsync();
        var diwanLine = world.AddLine(world.DiwanId, "D-1");

        await world.SendToInvestigationAsync();

        Assert.All(diwanLine.Visits, visit => Assert.True(visit.IsLocked));
    }

    [Fact]
    public async Task Redirect_LocksTheReceivedVisit_AndEditsAreRefused_UntilAnAdministratorCorrects()
    {
        var world = new World();
        await world.RegisterAsync();
        var form = world.PublishInvestigationForm();
        world.AddLine(world.InvestigationId, "INV-1");
        var movement = await world.SendToInvestigationAsync();
        var received = await world.ReceiveAsync(movement, ("case_no", "77/2026"));
        var visitId = received.Value.RegistryVisitId;

        var edited = await world.SetHandler(ScopeOf(world.InvestigationId)).Handle(
            new SetVisitRecordCommand(visitId, world.Investigator.UserId,
                new[] { new DocumentFieldValueInput("case_no", "78/2026"), new DocumentFieldValueInput("note", "تمت المراجعة") }),
            CancellationToken.None);
        Assert.True(edited.IsSuccess, edited.Error?.Code);
        Assert.True(edited.Value.CanEdit);

        var redirect = await new RedirectMovementCommandHandler(
                world.Movements, world.Departments, world.Users, world.Routing(), world.CustodyScope,
                world.UnitOfWork, FakeNotificationPublisher.Instance, world.Records())
            .Handle(new RedirectMovementCommand(movement.MovementId, world.Investigator.UserId, world.DiwanId), CancellationToken.None);
        Assert.True(redirect.IsSuccess, redirect.Error?.Code);
        Assert.True(world.Visit(visitId).IsLocked);

        var afterLock = await world.SetHandler(ScopeOf(world.InvestigationId)).Handle(
            new SetVisitRecordCommand(visitId, world.Investigator.UserId, new[] { new DocumentFieldValueInput("case_no", "79/2026") }),
            CancellationToken.None);
        Assert.Equal(DomainErrors.DepartmentRecords.VisitLocked.Code, afterLock.Error.Code);

        var correct = new CorrectVisitRecordCommandHandler(world.Numbers, world.Documents, world.RecordForms, world.Options, world.UnitOfWork);
        var noReason = await correct.Handle(
            new CorrectVisitRecordCommand(visitId, Admin, new[] { new DocumentFieldValueInput("case_no", "79/2026") }, " "),
            CancellationToken.None);
        Assert.Equal(DomainErrors.DepartmentRecords.CorrectionReasonRequired.Code, noReason.Error.Code);

        var corrected = await correct.Handle(
            new CorrectVisitRecordCommand(visitId, Admin, new[] { new DocumentFieldValueInput("case_no", "79/2026") }, "خطأ في النقل"),
            CancellationToken.None);
        Assert.True(corrected.IsSuccess, corrected.Error?.Code);
        var visit = world.Visit(visitId);
        Assert.Equal("خطأ في النقل", visit.CorrectionReason);
        Assert.Equal("79/2026", Assert.Single(visit.Values).TextValue);
        Assert.Equal(form.SchemaVersionId, visit.SchemaVersionId);
    }

    [Fact]
    public async Task SetRecord_FromAnotherDepartment_IsRefused()
    {
        var world = new World();
        await world.RegisterAsync();
        world.PublishInvestigationForm();
        world.AddLine(world.InvestigationId, "INV-1");
        var movement = await world.SendToInvestigationAsync();
        var received = await world.ReceiveAsync(movement, ("case_no", "77/2026"));

        var result = await world.SetHandler(ScopeOf(world.HrId)).Handle(
            new SetVisitRecordCommand(received.Value.RegistryVisitId, Admin, new[] { new DocumentFieldValueInput("case_no", "1") }),
            CancellationToken.None);

        Assert.Equal(DomainErrors.DepartmentRecords.NotOwnerDepartment.Code, result.Error.Code);
    }

    [Fact]
    public async Task ReturnToSender_LocksTheReturningRecord_AndGivesTheSenderANewVisit()
    {
        var world = new World();
        await world.RegisterAsync();
        world.PublishInvestigationForm();
        var diwanLine = world.AddLine(world.DiwanId, "D-1");
        world.AddLine(world.InvestigationId, "INV-1");
        var movement = await world.SendToInvestigationAsync();
        var received = await world.ReceiveAsync(movement, ("case_no", "77/2026"));

        var returned = await new ReturnMovementToSenderCommandHandler(
                world.Movements, world.CustodyScope, world.UnitOfWork, FakeNotificationPublisher.Instance, world.Records())
            .Handle(new ReturnMovementToSenderCommand(movement.MovementId, world.Investigator.UserId, "ناقص"), CancellationToken.None);

        Assert.True(returned.IsSuccess, returned.Error?.Code);
        Assert.True(world.Visit(received.Value.RegistryVisitId).IsLocked);
        Assert.Equal(2, diwanLine.Visits.Count);
        var handedBack = diwanLine.Visits.Single(visit => !visit.IsLocked);
        Assert.Equal(movement.Steps.Single().StepId, handedBack.MovementStepId);
        Assert.Equal(Today, handedBack.RegistryDate);
    }

    // ---- Visibility ----

    [Fact]
    public async Task Records_AreShownToTheirDepartment_AndToViewAll_ButNotToOthers()
    {
        var world = new World();
        await world.RegisterAsync();
        world.PublishInvestigationForm();
        world.AddLine(world.InvestigationId, "INV-1");
        var movement = await world.SendToInvestigationAsync();
        Assert.True((await world.ReceiveAsync(movement, ("case_no", "77/2026"))).IsSuccess);

        var own = await world.QueryHandler(ScopeOf(world.InvestigationId), Permissions.DocumentEdit)
            .Handle(new GetDocumentDepartmentRecordsQuery(world.Document.DocumentId), CancellationToken.None);
        var record = Assert.Single(own.Value.Records);
        Assert.Equal(world.InvestigationId, record.DepartmentId);
        var openVisit = record.Visits.Single(visit => visit.SchemaVersionId is not null);
        Assert.True(openVisit.CanEdit);
        Assert.Equal("77/2026", openVisit.Fields.Single(field => field.Field.Key == "case_no").Value);

        var other = await world.QueryHandler(ScopeOf(world.HrId))
            .Handle(new GetDocumentDepartmentRecordsQuery(world.Document.DocumentId), CancellationToken.None);
        Assert.Empty(other.Value.Records);

        var viewAll = await world.QueryHandler(ScopeOf(world.HrId), Permissions.DepartmentRecordViewAll)
            .Handle(new GetDocumentDepartmentRecordsQuery(world.Document.DocumentId), CancellationToken.None);
        var seen = Assert.Single(viewAll.Value.Records);
        Assert.All(seen.Visits, visit => Assert.False(visit.CanEdit));
    }

    // ---- Closing belongs to the creating department ----

    [Fact]
    public async Task Cancel_FromADepartmentThatDidNotCreateTheDocument_IsRefused()
    {
        var world = new World();
        await world.RegisterAsync();

        var result = await new CancelDocumentCommandHandler(
                world.Documents, world.Movements, new FakeDocumentAccessEvaluator { AllowAll = true }, world.UnitOfWork,
                ScopeOf(world.InvestigationId), world.Records())
            .Handle(new CancelDocumentCommand(world.Document.DocumentId, world.Investigator.UserId, "سبب"), CancellationToken.None);

        Assert.Equal(DomainErrors.Correspondence.OnlyOwningDepartmentCanClose.Code, result.Error.Code);
    }

    [Fact]
    public async Task Cancel_FromTheCreatingDepartment_LocksEveryOpenRecord()
    {
        var world = new World();
        await world.RegisterAsync();
        var line = world.AddLine(world.InvestigationId, "INV-1");

        var result = await new CancelDocumentCommandHandler(
                world.Documents, world.Movements, new FakeDocumentAccessEvaluator { AllowAll = true }, world.UnitOfWork,
                ScopeOf(world.DiwanId), world.Records())
            .Handle(new CancelDocumentCommand(world.Document.DocumentId, world.Clerk.UserId, "سبب"), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.All(line.Visits, visit => Assert.True(visit.IsLocked));
    }

    // ---- Schema management keeps forms apart ----

    [Fact]
    public async Task PublishingADepartmentForm_DoesNotRetireTheCreationForm()
    {
        var versions = new FakeDocumentTypeSchemaVersionRepository();
        var creation = DocumentTypeSchemaVersion.CreateDraft(DocumentType.DecisionId, 1, Admin).Value;
        Assert.True(creation.Publish(Admin).IsSuccess);
        versions.Add(creation);

        var department = Guid.NewGuid();
        var departmentForm = DocumentTypeSchemaVersion.CreateDraft(DocumentType.DecisionId, 1, Admin, departmentId: department).Value;
        versions.Add(departmentForm);

        var result = await new PublishSchemaVersionCommandHandler(versions, new FakeUnitOfWork())
            .Handle(new PublishSchemaVersionCommand(departmentForm.SchemaVersionId, Admin), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(DocumentTypeSchemaVersionStatus.Published, creation.Status);
        Assert.Same(creation, await versions.GetPublishedAsync(DocumentType.DecisionId));
        Assert.Same(departmentForm, await versions.GetPublishedAsync(DocumentType.DecisionId, department));
    }
}
