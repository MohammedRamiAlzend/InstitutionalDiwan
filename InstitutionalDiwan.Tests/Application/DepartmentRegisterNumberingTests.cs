using InstitutionalDiwan.Application.Features.Movements;
using InstitutionalDiwan.Application.Features.Numbering.Numbers;
using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DepartmentRegisterNumberingTests
{
    private const int NumberingYear = 2026;
    private static readonly DateTime FirstVisit = new(2026, 3, 14);
    private static readonly DateTime SecondVisit = new(2026, 5, 1);

    private sealed class World
    {
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentNumberRepository Numbers { get; } = new();
        public FakeNumberingRegistryRepository Registries { get; } = new();
        public FakeDepartmentRepository Departments { get; } = new();
        public FakeDocumentMovementRepository Movements { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();

        public Guid UserId { get; } = Guid.NewGuid();
        public Guid DiwanId { get; }
        public Guid HrId { get; }
        public Guid FinanceId { get; }
        public Document Document { get; }
        public DocumentNumberResponse CentralNumber { get; private set; } = null!;

        public World(bool seedDepartmentRegistries = true)
        {
            DiwanId = AddDepartment("DIWAN");
            HrId = AddDepartment("HR");
            FinanceId = AddDepartment("FIN");

            var document = Document.CreateDraft(
                "طلب إحاطة",
                DocumentType.InternalMemoId,
                DocumentDirection.InternalId,
                DiwanId,
                UserId);

            Assert.True(document.IsSuccess);
            Document = document.Value;
            Documents.Add(Document);

            if (seedDepartmentRegistries)
            {
                Registries.Add(CreateRegistry("HR", "سجل الموارد البشرية", HrId));
                Registries.Add(CreateRegistry("FIN", "سجل الشؤون المالية", FinanceId));
            }
        }

        public Guid AddDepartment(string code)
        {
            var department = Department.Create(code, $"دائرة {code}", 1);
            Assert.True(department.IsSuccess);
            Departments.Add(department.Value);
            return department.Value.DepartmentId;
        }

        public NumberingRegistry CreateRegistry(string code, string nameAr, Guid departmentId)
        {
            var registry = NumberingRegistry.Create(code, nameAr, departmentId: departmentId);
            Assert.True(registry.IsSuccess);
            return registry.Value;
        }

        public async Task<DocumentNumberResponse> IssueCentralAsync()
        {
            var result = await new IssueOfficialNumberCommandHandler(
                    Documents, Numbers, Registries, UnitOfWork)
                .Handle(new IssueOfficialNumberCommand(Document.DocumentId, UserId, NumberingYear),
                    CancellationToken.None);

            Assert.True(result.IsSuccess, result.Error?.Code);
            CentralNumber = result.Value;
            return result.Value;
        }

        public RecordDepartmentRegistrationCommandHandler RecordHandler() =>
            new(Documents, Numbers, Departments, Registries, UnitOfWork);

        public Task<Result<DocumentNumberResponse>> RecordAsync(
            Guid departmentId,
            string numberText,
            DateTime registryDate) =>
            RecordHandler().Handle(
                new RecordDepartmentRegistrationCommand(
                    Document.DocumentId, departmentId, numberText, registryDate, UserId),
                CancellationToken.None);

        public ReceiveDocumentCommandHandler ReceiveHandler() =>
            new(Movements, Documents, UnitOfWork);

        public DocumentNumber? DepartmentEntry(Guid departmentId) =>
            Numbers.DocumentNumbers.SingleOrDefault(number =>
                number.ScopeId == DocumentNumberScope.DepartmentId &&
                number.DepartmentId == departmentId);
    }

    [Fact]
    public async Task Record_StoresTypedNumberAndDate_WithoutTouchingCentralNumber()
    {
        var world = new World();
        var central = await world.IssueCentralAsync();

        var result = await world.RecordAsync(world.HrId, "45", FirstVisit);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal("45", result.Value.OfficialNumberText);
        Assert.Equal(DocumentNumberScope.DepartmentId, result.Value.ScopeId);
        Assert.Equal(world.HrId, result.Value.DepartmentId);
        Assert.Equal(0, result.Value.SequenceValue);
        Assert.Equal(NumberingYear, result.Value.NumberingYear);
        Assert.Equal(FirstVisit, result.Value.RegistryDate);

        var registration = result.Value.History.Single();
        Assert.Equal(DocumentNumberEventType.RegisteredInDepartmentId, registration.EventTypeId);
        Assert.Equal(FirstVisit, registration.EffectiveDate);

        Assert.Equal(central.OfficialNumberText, world.Document.OfficialNumberText);
        Assert.Equal(central.DocumentNumberId, world.Document.ActiveOfficialNumberId);
    }

    [Fact]
    public async Task Record_WhenDepartmentAlreadyRegistered_KeepsNumberAndAddsNewDate()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.RecordAsync(world.HrId, "45", FirstVisit);

        var result = await world.RecordAsync(world.HrId, "45", SecondVisit);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal("45", result.Value.OfficialNumberText);
        Assert.Equal(SecondVisit, result.Value.RegistryDate);
        Assert.Equal(2, result.Value.History.Count);
        Assert.Contains(result.Value.History,
            history => history.EventTypeId == DocumentNumberEventType.ReRegisteredInDepartmentId &&
                       history.EffectiveDate == SecondVisit);

        Assert.Single(world.Numbers.DocumentNumbers, number => number.IsDepartmentEntry);
    }

    [Fact]
    public async Task Record_WhenNumberDiffersOnReturn_ReturnsConflict()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.RecordAsync(world.HrId, "45", FirstVisit);

        var result = await world.RecordAsync(world.HrId, "46", SecondVisit);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.DepartmentNumberLocked.Code, result.Error.Code);
        Assert.Single(world.Numbers.DocumentNumbers, number => number.IsDepartmentEntry);
    }

    [Fact]
    public async Task Record_InDifferentDepartments_AllowsTheSameNumber()
    {
        var world = new World();
        await world.IssueCentralAsync();

        var hr = await world.RecordAsync(world.HrId, "45", FirstVisit);
        var finance = await world.RecordAsync(world.FinanceId, "45", FirstVisit);

        Assert.True(hr.IsSuccess, hr.Error?.Code);
        Assert.True(finance.IsSuccess, finance.Error?.Code);
        Assert.NotEqual(hr.Value.DocumentNumberId, finance.Value.DocumentNumberId);
        Assert.NotEqual(hr.Value.RegistryId, finance.Value.RegistryId);
    }

    [Fact]
    public async Task Record_WhenDepartmentHasNoRegistry_ReturnsConflict()
    {
        var world = new World(seedDepartmentRegistries: false);
        await world.IssueCentralAsync();

        var result = await world.RecordAsync(world.HrId, "45", FirstVisit);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.DepartmentRegistryNotConfigured.Code, result.Error.Code);
    }

    [Fact]
    public async Task Record_WhenDraftHasNoCentralNumber_ReturnsConflict()
    {
        var world = new World();

        var result = await world.RecordAsync(world.HrId, "45", FirstVisit);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.DocumentNotRegistered.Code, result.Error.Code);
    }

    [Fact]
    public async Task Record_WhenNumberAlreadyUsedInThatRegister_ReturnsConflict()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.RecordAsync(world.HrId, "45", FirstVisit);

        var second = Document.CreateDraft(
            "طلب آخر", DocumentType.InternalMemoId, DocumentDirection.InternalId, world.DiwanId, world.UserId);
        Assert.True(second.IsSuccess);
        world.Documents.Add(second.Value);

        var issue = await new IssueOfficialNumberCommandHandler(
                world.Documents, world.Numbers, world.Registries, world.UnitOfWork)
            .Handle(new IssueOfficialNumberCommand(second.Value.DocumentId, world.UserId, NumberingYear),
                CancellationToken.None);
        Assert.True(issue.IsSuccess, issue.Error?.Code);

        var result = await world.RecordHandler().Handle(
            new RecordDepartmentRegistrationCommand(
                second.Value.DocumentId, world.HrId, "45", SecondVisit, world.UserId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.RegistryNumberTaken.Code, result.Error.Code);
    }

    [Fact]
    public async Task CancelCentral_StillFindsTheCentralNumberWhenDepartmentEntriesExist()
    {
        var world = new World();
        var central = await world.IssueCentralAsync();
        await world.RecordAsync(world.HrId, "45", FirstVisit);

        var result = await new CancelOfficialNumberCommandHandler(world.Numbers, world.UnitOfWork)
            .Handle(new CancelOfficialNumberCommand(
                world.Document.DocumentId, central.DocumentNumberId, world.UserId, "خطأ"),
                CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(DocumentNumberStatus.CancelledId, result.Value.StatusId);
    }

    [Fact]
    public async Task Receive_WithoutDepartmentRegistration_IsBlocked()
    {
        var world = new World();
        await world.IssueCentralAsync();

        var movement = CreateSentMovement(world);

        var result = await world.ReceiveHandler().Handle(
            new ReceiveDocumentCommand(movement.MovementId, world.UserId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.DepartmentRegistrationRequired.Code, result.Error.Code);
        Assert.Equal(DocumentMovement.StatusSent, movement.StatusId);
    }

    [Fact]
    public async Task Receive_AfterDepartmentRegistration_RecordsTheRegisterNumber()
    {
        var world = new World();
        await world.IssueCentralAsync();
        var entry = await world.RecordAsync(world.HrId, "45", FirstVisit);
        Assert.True(entry.IsSuccess, entry.Error?.Code);

        var movement = CreateSentMovement(world);

        var result = await world.ReceiveHandler().Handle(
            new ReceiveDocumentCommand(movement.MovementId, world.UserId), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(entry.Value.DocumentNumberId, result.Value.RegistryNumberId);
        Assert.Equal("45", result.Value.RegistryNumberText);
        Assert.Equal(FirstVisit, result.Value.RegistryDate);
        Assert.Equal(DocumentMovement.StatusReceived, movement.StatusId);

        var step = movement.Steps.Single(current => current.StepNo == movement.CurrentStepNo);
        Assert.Equal(entry.Value.DocumentNumberId, step.RegistryNumberId);
    }

    [Fact]
    public async Task DepartmentRegister_OrdersLinesByDepartmentDate()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.RecordAsync(world.HrId, "45", FirstVisit);

        var result = await new ListDepartmentRegisterQueryHandler(
                world.Departments, world.Documents, world.Numbers)
            .Handle(new ListDepartmentRegisterQuery(world.HrId, NumberingYear), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        var line = Assert.Single(result.Value);
        Assert.Equal("45", line.RegistryNumberText);
        Assert.Equal(FirstVisit, line.RegistryDate);
        Assert.Equal(world.Document.OfficialNumberText, line.CentralNumberText);
        Assert.Equal(1, line.RegistrationCount);
    }

    private static DocumentMovement CreateSentMovement(World world)
    {
        var movement = DocumentMovement.Create(
            world.Document.DocumentId, world.DiwanId, world.HrId, world.UserId);

        Assert.True(movement.IsSuccess);
        world.Movements.Add(movement.Value);

        var send = movement.Value.Send(world.UserId);
        Assert.True(send.IsSuccess, send.Error?.Code);
        return movement.Value;
    }
}
