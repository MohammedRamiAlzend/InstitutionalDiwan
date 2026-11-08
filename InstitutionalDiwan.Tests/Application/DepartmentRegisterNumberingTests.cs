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
    private static readonly DateTime NextYearVisit = new(2027, 2, 3);

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
                    Documents, Numbers, Registries, new FakeDocumentAccessEvaluator { AllowAll = true }, UnitOfWork, TestClock.Instance)
                .Handle(new IssueOfficialNumberCommand(Document.DocumentId, UserId, NumberingYear),
                    CancellationToken.None);

            Assert.True(result.IsSuccess, result.Error?.Code);
            CentralNumber = result.Value;
            return result.Value;
        }

        public RecordDepartmentRegistrationCommandHandler RecordHandler() =>
            new(Documents, RegistrationService(), UnitOfWork);

        public DepartmentRegistrationService RegistrationService() =>
            new(Numbers, Departments, Registries, CustodyScope, UnitOfWork);

        public FakeDepartmentScopeEvaluator CustodyScope { get; } = new() { InstitutionWide = true };

        public Task<Result<DocumentNumberResponse>> RecordAsync(
            Guid departmentId,
            string numberText,
            DateTime registryDate) =>
            RecordHandler().Handle(
                new RecordDepartmentRegistrationCommand(
                    Document.DocumentId, departmentId, numberText, registryDate, UserId),
                CancellationToken.None);

        public ReceiveDocumentCommandHandler ReceiveHandler() =>
            new(Movements, Documents, CustodyScope, UnitOfWork, FakeNotificationPublisher.Instance, RegistrationService(), TestClock.Instance);

        public Task<Result<MovementStepReceiptResponse>> ReceiveAsync(
            DocumentMovement movement,
            DateTime registryDate,
            string? numberText = null) =>
            ReceiveHandler().Handle(
                new ReceiveDocumentCommand(
                    movement.MovementId, UserId, RegistryDate: registryDate, RegistryNumberText: numberText),
                CancellationToken.None);

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
    public async Task Record_ForAnotherDepartment_IsRefusedEvenWhenTheScopeCoversIt()
    {
        var world = new World();
        await world.IssueCentralAsync();
        world.CustodyScope.PrimaryDepartmentId = world.FinanceId;

        var result = await world.RecordAsync(world.HrId, "45", FirstVisit);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.DepartmentRegistrationOwnDepartmentOnly.Code, result.Error.Code);
        Assert.Null(world.DepartmentEntry(world.HrId));

        var own = await world.RecordAsync(world.FinanceId, "7", FirstVisit);
        Assert.True(own.IsSuccess, own.Error?.Code);
    }

    [Fact]
    public async Task Record_WithAChosenRegister_StoresTheNumberInThatRegister()
    {
        var world = new World();
        await world.IssueCentralAsync();
        var second = world.CreateRegistry("HR-OUT", "سجل الصادر", world.HrId);
        world.Registries.Add(second);

        var result = await world.RecordHandler().Handle(
            new RecordDepartmentRegistrationCommand(
                world.Document.DocumentId, world.HrId, "45", FirstVisit, world.UserId,
                RegistryId: second.RegistryId),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(second.RegistryId, result.Value.RegistryId);
    }

    [Fact]
    public async Task Record_WithARegisterOfAnotherDepartment_IsRefused()
    {
        var world = new World();
        await world.IssueCentralAsync();
        var financeRegistry = world.Registries.Registries.Single(r => r.DepartmentId == world.FinanceId);

        var result = await world.RecordHandler().Handle(
            new RecordDepartmentRegistrationCommand(
                world.Document.DocumentId, world.HrId, "45", FirstVisit, world.UserId,
                RegistryId: financeRegistry.RegistryId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.DepartmentRegistryMismatch.Code, result.Error.Code);
    }

    [Fact]
    public async Task Record_WithAnUnknownRegister_ReportsNotFound()
    {
        var world = new World();
        await world.IssueCentralAsync();

        var result = await world.RecordHandler().Handle(
            new RecordDepartmentRegistrationCommand(
                world.Document.DocumentId, world.HrId, "45", FirstVisit, world.UserId,
                RegistryId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.RegistryNotFound.Code, result.Error.Code);
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
                world.Documents, world.Numbers, world.Registries,
                new FakeDocumentAccessEvaluator { AllowAll = true }, world.UnitOfWork, TestClock.Instance)
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

        var result = await new CancelOfficialNumberCommandHandler(
                world.Documents, world.Numbers, new FakeDocumentAccessEvaluator { AllowAll = true }, world.UnitOfWork)
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

        var result = await world.ReceiveAsync(movement, SecondVisit);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(entry.Value.DocumentNumberId, result.Value.RegistryNumberId);
        Assert.Equal("45", result.Value.RegistryNumberText);
        Assert.Equal(SecondVisit, result.Value.RegistryDate);
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

        var scope = new FakeDepartmentScopeEvaluator { InstitutionWide = true };
        var result = await RegisterHandler(world, scope)
            .Handle(new ListDepartmentRegisterQuery(world.HrId, NumberingYear), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        var line = Assert.Single(result.Value);
        Assert.Equal("45", line.RegistryNumberText);
        Assert.Equal(FirstVisit, line.RegistryDate);
        Assert.Equal(world.Document.OfficialNumberText, line.CentralNumberText);
        Assert.Equal(1, line.RegistrationCount);
    }

    [Fact]
    public async Task RecordRegistration_ForADepartmentOutsideTheCallersScope_IsForbidden()
    {
        var world = new World();
        await world.IssueCentralAsync();
        world.CustodyScope.InstitutionWide = false;
        world.CustodyScope.AccessibleDepartments.Add(world.DiwanId);

        var result = await world.RecordAsync(world.HrId, "45", FirstVisit);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DepartmentOutOfScope.Code, result.Error.Code);
        Assert.Null(world.DepartmentEntry(world.HrId));

        world.CustodyScope.AccessibleDepartments.Add(world.HrId);
        var allowed = await world.RecordAsync(world.HrId, "45", FirstVisit);

        Assert.True(allowed.IsSuccess, allowed.Error?.Code);
    }

    [Fact]
    public async Task DepartmentRegister_OutsideTheCallersScope_IsForbidden()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.RecordAsync(world.HrId, "45", FirstVisit);

        var result = await RegisterHandler(world, new FakeDepartmentScopeEvaluator())
            .Handle(new ListDepartmentRegisterQuery(world.HrId, NumberingYear), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Correspondence.DepartmentOutOfScope.Code, result.Error.Code);
    }

    [Fact]
    public async Task DepartmentRegister_KeepsALineTheCallerCannotView_WithoutItsContent()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.RecordAsync(world.HrId, "45", FirstVisit);
        var scope = new FakeDepartmentScopeEvaluator();
        scope.AccessibleDepartments.Add(world.HrId);

        var result = await RegisterHandler(world, scope)
            .Handle(new ListDepartmentRegisterQuery(world.HrId, NumberingYear), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        var line = Assert.Single(result.Value);
        Assert.Equal("45", line.RegistryNumberText);
        Assert.Equal(string.Empty, line.DocumentTitle);
        Assert.Null(line.CentralNumberText);
    }

    private static ListDepartmentRegisterQueryHandler RegisterHandler(World world, FakeDepartmentScopeEvaluator scope) =>
        new(
            world.Departments,
            world.Documents,
            world.Numbers,
            scope,
            new FakeUserContext { IsAuthenticated = true, UserId = Guid.NewGuid() },
            TestClock.Instance);

    [Fact]
    public async Task Receive_WithAChosenRegister_WritesTheNumberInThatRegister()
    {
        var world = new World();
        await world.IssueCentralAsync();
        var second = world.CreateRegistry("HR2", "سجل الموارد الثاني", world.HrId);
        world.Registries.Add(second);

        var result = await world.ReceiveHandler().Handle(
            new ReceiveDocumentCommand(
                CreateSentMovement(world).MovementId,
                world.UserId,
                RegistryDate: FirstVisit,
                RegistryNumberText: "45",
                RegistryId: second.RegistryId),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(second.RegistryId, world.DepartmentEntry(world.HrId)!.RegistryId);
    }

    [Fact]
    public async Task Receive_WithAnotherDepartmentsRegister_ReturnsConflict()
    {
        var world = new World();
        await world.IssueCentralAsync();
        var financeRegistry = world.Registries.Registries.Single(registry => registry.DepartmentId == world.FinanceId);

        var result = await world.ReceiveHandler().Handle(
            new ReceiveDocumentCommand(
                CreateSentMovement(world).MovementId,
                world.UserId,
                RegistryDate: FirstVisit,
                RegistryNumberText: "45",
                RegistryId: financeRegistry.RegistryId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.DepartmentRegistryMismatch.Code, result.Error.Code);
    }

    [Fact]
    public async Task Receive_OnReturnToADepartment_KeepsTheNumberAndAddsAVisit()
    {
        var world = new World();
        await world.IssueCentralAsync();

        var first = await world.ReceiveAsync(CreateSentMovement(world), FirstVisit, "45");
        Assert.True(first.IsSuccess, first.Error?.Code);
        Assert.False(first.Value.IsReturnVisit);

        var returnMovement = CreateSentMovement(world);
        var second = await world.ReceiveAsync(returnMovement, SecondVisit);

        Assert.True(second.IsSuccess, second.Error?.Code);
        Assert.True(second.Value.IsReturnVisit);
        Assert.Equal("45", second.Value.RegistryNumberText);
        Assert.Equal(first.Value.RegistryNumberId, second.Value.RegistryNumberId);

        var entry = world.DepartmentEntry(world.HrId)!;
        Assert.Equal(new[] { FirstVisit, SecondVisit }, entry.Visits.Select(visit => visit.RegistryDate).OrderBy(date => date));
        Assert.Equal(SecondVisit, entry.RegistryDate);

        var step = returnMovement.Steps.Single();
        Assert.Equal(second.Value.RegistryVisitId, step.RegistryVisitId);
        Assert.Equal(step.StepId, entry.Visits.Single(visit => visit.RegistryDate == SecondVisit).MovementStepId);
    }

    [Fact]
    public async Task Receive_OnReturnWithTheSameDate_IsRefusedAndLeavesTheStepWaiting()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.ReceiveAsync(CreateSentMovement(world), FirstVisit, "45");

        var returnMovement = CreateSentMovement(world);
        var result = await world.ReceiveAsync(returnMovement, FirstVisit);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.VisitDateDuplicate.Code, result.Error.Code);
        Assert.Equal(DocumentMovement.StatusSent, returnMovement.StatusId);
        Assert.Single(world.DepartmentEntry(world.HrId)!.Visits);
    }

    [Fact]
    public async Task Receive_OnTheDateTheClerkJustRegisteredForThisStep_UsesThatVisit()
    {
        var world = new World();
        await world.IssueCentralAsync();
        var movement = CreateSentMovement(world);
        world.UnitOfWork.UtcNow = DateTime.UtcNow.AddMinutes(1);
        var entry = await world.RecordAsync(world.HrId, "45", FirstVisit);
        Assert.True(entry.IsSuccess, entry.Error?.Code);

        var result = await world.ReceiveAsync(movement, FirstVisit);

        Assert.True(result.IsSuccess, result.Error?.Code);
        var visit = Assert.Single(world.DepartmentEntry(world.HrId)!.Visits);
        Assert.Equal(movement.Steps.Single().StepId, visit.MovementStepId);
        Assert.Equal(visit.VisitId, result.Value.RegistryVisitId);
    }

    [Fact]
    public async Task Record_WithADateAlreadyInTheRegister_ReturnsConflict()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.RecordAsync(world.HrId, "45", FirstVisit);

        var result = await world.RecordAsync(world.HrId, "45", FirstVisit);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.VisitDateDuplicate.Code, result.Error.Code);
    }

    [Fact]
    public async Task Receive_InANewYearWithoutANumber_RequiresRegistration()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.ReceiveAsync(CreateSentMovement(world), FirstVisit, "45");

        var result = await world.ReceiveAsync(CreateSentMovement(world), NextYearVisit);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.DepartmentRegistrationRequired.Code, result.Error.Code);
    }

    [Fact]
    public async Task Receive_InANewYearWithANumber_OpensALineForThatYear()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.ReceiveAsync(CreateSentMovement(world), FirstVisit, "45");

        var result = await world.ReceiveAsync(CreateSentMovement(world), NextYearVisit, "7");

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.False(result.Value.IsReturnVisit);
        Assert.Equal(
            new[] { ("45", 2026), ("7", 2027) },
            world.Numbers.DocumentNumbers
                .Where(number => number.IsDepartmentEntry)
                .Select(number => (number.OfficialNumberText, number.NumberingYear))
                .OrderBy(line => line.NumberingYear));
    }

    [Fact]
    public async Task Receive_WithADifferentNumberOnAnExistingLine_ReturnsConflict()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.ReceiveAsync(CreateSentMovement(world), FirstVisit, "45");

        var result = await world.ReceiveAsync(CreateSentMovement(world), SecondVisit, "46");

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Numbering.DepartmentNumberLocked.Code, result.Error.Code);
    }

    [Fact]
    public async Task Receive_WithANumberAnotherDocumentHoldsThatYear_ReturnsConflict()
    {
        var world = new World();
        await world.IssueCentralAsync();
        await world.ReceiveAsync(CreateSentMovement(world), FirstVisit, "45");
        var entry = world.DepartmentEntry(world.HrId)!;

        var other = Document.CreateDraft(
            "كتاب آخر", DocumentType.InternalMemoId, DocumentDirection.InternalId, world.DiwanId, world.UserId).Value;
        world.Documents.Add(other);
        var issued = await new IssueOfficialNumberCommandHandler(
                world.Documents, world.Numbers, world.Registries,
                new FakeDocumentAccessEvaluator { AllowAll = true }, world.UnitOfWork, TestClock.Instance)
            .Handle(new IssueOfficialNumberCommand(other.DocumentId, world.UserId, NumberingYear), CancellationToken.None);
        Assert.True(issued.IsSuccess, issued.Error?.Code);
        var otherMovement = DocumentMovement.Create(other.DocumentId, world.DiwanId, world.HrId, world.UserId).Value;
        world.Movements.Add(otherMovement);
        Assert.True(otherMovement.Send(world.UserId).IsSuccess);

        var sameYear = await world.ReceiveAsync(otherMovement, SecondVisit, entry.OfficialNumberText);
        Assert.Equal(DomainErrors.Numbering.RegistryNumberTaken.Code, sameYear.Error.Code);

        var otherYear = await world.ReceiveAsync(otherMovement, NextYearVisit, entry.OfficialNumberText);
        Assert.True(otherYear.IsSuccess, otherYear.Error?.Code);
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
