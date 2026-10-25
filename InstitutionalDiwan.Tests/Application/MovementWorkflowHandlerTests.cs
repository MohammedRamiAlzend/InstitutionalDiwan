using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Application.Features.Movements;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class MovementWorkflowHandlerTests
{
    private sealed class World
    {
        public FakeUserRepository Users { get; } = new();
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentNumberRepository Numbers { get; } = new();
        public FakeNumberingRegistryRepository Registries { get; } = new();
        public FakeDepartmentRepository Departments { get; } = new();
        public FakeInstitutionSettingRepository Settings { get; } = new();
        public FakeDocumentMovementRepository Movements { get; } = new();
        public FakeDepartmentScopeEvaluator Scope { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();

        public User Clerk { get; }
        public User HROfficer { get; }
        public Guid DiwanId { get; }
        public Guid HrId { get; }
        public Document Document { get; }

        public World()
        {
            DiwanId = AddDepartment("DIWAN");
            HrId = AddDepartment("HR");

            var settings = InstitutionSetting.Create(
                "المؤسسة", 1, 1, centralDiwanDepartmentId: DiwanId);
            Assert.True(settings.IsSuccess);
            Settings.Add(settings.Value);

            var clerk = User.Create("clerk", "hash", DiwanId);
            Assert.True(clerk.IsSuccess);
            Clerk = clerk.Value;
            Users.Add(Clerk);

            var hrOfficer = User.Create("hr.officer", "hash", HrId);
            Assert.True(hrOfficer.IsSuccess);
            HROfficer = hrOfficer.Value;
            Users.Add(HROfficer);

            var document = Document.CreateDraft(
                "طلب إحاطة",
                DocumentType.InternalMemoId,
                DocumentDirection.InternalId,
                DiwanId,
                Clerk.UserId);
            Assert.True(document.IsSuccess);
            Document = document.Value;
            Documents.Add(Document);

            Registries.Add(NumberingRegistry.Create("HR", "سجل الموارد البشرية", departmentId: HrId).Value);
        }

        public Guid AddDepartment(string code)
        {
            var department = Department.Create(code, $"دائرة {code}", 1);
            Assert.True(department.IsSuccess);
            Departments.Add(department.Value);
            return department.Value.DepartmentId;
        }

        public CentralDiwanRouting Routing() => new(Settings);

        public CreateMovementCommandHandler CreateMovementHandler() =>
            new(Movements, Documents, Departments, Users, Routing(), UnitOfWork);

        public SendMovementCommandHandler SendMovementHandler() =>
            new(Movements, Routing(), UnitOfWork);

        public ReceiveDocumentCommandHandler ReceiveHandler() =>
            new(Movements, Documents, UnitOfWork, TestClock.Instance);

        public CompleteMovementStepCommandHandler CompleteHandler() =>
            new(Movements, UnitOfWork);

        public RejectMovementStepCommandHandler RejectHandler() =>
            new(Movements, Routing(), UnitOfWork);

        public ReturnMovementToSenderCommandHandler ReturnHandler() =>
            new(Movements, UnitOfWork);

        public RedirectMovementCommandHandler RedirectHandler() =>
            new(Movements, Departments, Users, Routing(), UnitOfWork);

        public CloseMovementCommandHandler CloseHandler() =>
            new(Movements, Documents, UnitOfWork);

        public CancelMovementCommandHandler CancelHandler() =>
            new(Movements, Documents, UnitOfWork);

        public GetMovementByIdQueryHandler GetByIdHandler() =>
            new(Movements);

        public ListDocumentMovementsQueryHandler ListByDocumentHandler() =>
            new(Movements, Documents);

        public GetDepartmentInboxQueryHandler InboxHandler() =>
            new(Movements, Documents, Users, Scope);

        public GetDepartmentOutboxQueryHandler OutboxHandler() =>
            new(Movements, Documents, Users, Scope);
    }

    private static async Task<Document> RegisterDocument(World world)
    {
        var result = await new RegisterDocumentCommandHandler(
                world.Documents, world.Numbers, world.Registries, world.UnitOfWork, TestClock.Instance)
            .Handle(new RegisterDocumentCommand(world.Document.DocumentId, world.Clerk.UserId, 2026),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        return world.Documents.Documents.Single(document => document.DocumentId == world.Document.DocumentId);
    }

    private static async Task<MovementResponse> CreateMovement(World world)
    {
        var result = await world.CreateMovementHandler().Handle(
            new CreateMovementCommand(world.Document.DocumentId, world.Clerk.UserId, world.HrId),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        return result.Value;
    }

    [Fact]
    public async Task CreateMovement_RegisteredDocumentOpensMovementAndMarksDocumentInMovement()
    {
        var world = new World();
        await RegisterDocument(world);

        var movement = await CreateMovement(world);

        Assert.Equal(world.Document.DocumentId, movement.DocumentId);
        Assert.Equal(world.DiwanId, movement.SourceDepartmentId);
        Assert.Equal(world.HrId, movement.DestinationDepartmentId);
        Assert.Equal(MovementStatus.DraftId, movement.StatusId);
        Assert.Single(movement.Steps);
        Assert.Equal(DocumentStatusType.InMovementId, world.Document.CurrentStatusId);
        Assert.True(await world.Movements.HasActiveMovementsAsync(world.Document.DocumentId));
    }

    [Fact]
    public async Task CreateMovement_DraftDocument_ReturnsOnlyRegisteredRoutable()
    {
        var world = new World();

        var result = await world.CreateMovementHandler().Handle(
            new CreateMovementCommand(world.Document.DocumentId, world.Clerk.UserId, world.HrId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Correspondence.OnlyRegisteredRoutable.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateMovement_WhenActiveMovementExists_ReturnsConflict()
    {
        var world = new World();
        await RegisterDocument(world);
        await CreateMovement(world);

        var result = await world.CreateMovementHandler().Handle(
            new CreateMovementCommand(world.Document.DocumentId, world.Clerk.UserId, world.HrId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Movements.ActiveMovementExists.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateMovement_BetweenTwoOrdinaryDepartments_IsRefused()
    {
        var world = new World();
        var financeId = world.AddDepartment("FIN");
        await RegisterDocument(world);

        var result = await world.CreateMovementHandler().Handle(
            new CreateMovementCommand(world.Document.DocumentId, world.HROfficer.UserId, financeId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Movements.CentralDiwanRoutingRequired.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateMovement_FromAnOrdinaryDepartmentToTheCentralDiwan_IsAllowed()
    {
        var world = new World();
        await RegisterDocument(world);

        var result = await world.CreateMovementHandler().Handle(
            new CreateMovementCommand(world.Document.DocumentId, world.HROfficer.UserId, world.DiwanId),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(world.HrId, result.Value.SourceDepartmentId);
        Assert.Equal(world.DiwanId, result.Value.DestinationDepartmentId);
    }

    [Fact]
    public async Task Redirect_FromAnOrdinaryDepartmentToAnotherOrdinaryDepartment_IsRefused()
    {
        var world = new World();
        var financeId = world.AddDepartment("FIN");
        await RegisterDocument(world);
        var movement = await CreateMovement(world);
        await world.SendMovementHandler().Handle(
            new SendMovementCommand(movement.MovementId, world.Clerk.UserId),
            CancellationToken.None);

        var result = await world.RedirectHandler().Handle(
            new RedirectMovementCommand(
                movement.MovementId, world.HROfficer.UserId, financeId, null, "تحويل"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Movements.CentralDiwanRoutingRequired.Code, result.Error.Code);
    }

    [Fact]
    public async Task Receive_WithANonCurrentStep_IsRefused()
    {
        var world = new World();
        await RegisterDocument(world);
        var movement = await CreateMovement(world);
        await world.SendMovementHandler().Handle(
            new SendMovementCommand(movement.MovementId, world.Clerk.UserId),
            CancellationToken.None);

        var result = await world.ReceiveHandler().Handle(
            new ReceiveDocumentCommand(
                movement.MovementId, world.HROfficer.UserId, StepId: Guid.NewGuid()),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Movements.StepNotCurrent.Code, result.Error.Code);
    }

    [Fact]
    public async Task SendReceiveComplete_ClosesMovementAndReleasesDocument()
    {
        var world = new World();
        await RegisterDocument(world);
        var movement = await CreateMovement(world);

        var sent = await world.SendMovementHandler().Handle(
            new SendMovementCommand(movement.MovementId, world.Clerk.UserId, "يرجى المعالجة"),
            CancellationToken.None);
        Assert.True(sent.IsSuccess);
        Assert.Equal(MovementStatus.SentId, sent.Value.StatusId);

        // Destination must register the document in its department register before receiving.
        var registryNumber = DocumentNumber.CreateDepartmentEntry(
            world.Document.DocumentId,
            world.HrId,
            world.Registries.Registries.Single(r => r.DepartmentId == world.HrId).RegistryId,
            "HR-77",
            DateTime.UtcNow,
            world.HROfficer.UserId);
        Assert.True(registryNumber.IsSuccess);
        world.Numbers.Add(registryNumber.Value);
        world.Document.AddDepartmentNumber(registryNumber.Value);

        var received = await world.ReceiveHandler().Handle(
            new ReceiveDocumentCommand(movement.MovementId, world.HROfficer.UserId, "تم الاستلام"),
            CancellationToken.None);
        Assert.True(received.IsSuccess);

        var completed = await world.CompleteHandler().Handle(
            new CompleteMovementStepCommand(movement.MovementId, world.HROfficer.UserId, "انتهى"),
            CancellationToken.None);
        Assert.True(completed.IsSuccess);

        var closed = await world.CloseHandler().Handle(
            new CloseMovementCommand(movement.MovementId, world.Clerk.UserId, "مغلق"),
            CancellationToken.None);
        Assert.True(closed.IsSuccess);
        Assert.Equal(MovementStatus.ClosedId, closed.Value.StatusId);
        Assert.Equal(DocumentStatusType.RegisteredId, world.Document.CurrentStatusId);
    }

    [Fact]
    public async Task RejectMovement_RoutesTheDocumentBackToTheCentralDiwan()
    {
        var world = new World();
        await RegisterDocument(world);
        var movement = await CreateMovement(world);
        await world.SendMovementHandler().Handle(
            new SendMovementCommand(movement.MovementId, world.Clerk.UserId),
            CancellationToken.None);

        var result = await world.RejectHandler().Handle(
            new RejectMovementStepCommand(movement.MovementId, world.HROfficer.UserId, "غير مطابق"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(MovementStatus.SentId, result.Value.StatusId);
        Assert.Equal(2, result.Value.CurrentStepNo);
        Assert.Equal(world.DiwanId, result.Value.DestinationDepartmentId);

        var rejectedStep = Assert.Single(
            result.Value.Steps,
            step => step.StepStatusId == MovementStepStatus.RejectedId);
        Assert.Equal(1, rejectedStep.StepNo);

        var returnStep = Assert.Single(
            result.Value.Steps,
            step => step.StepNo == result.Value.CurrentStepNo);
        Assert.Equal(2, returnStep.StepNo);
        Assert.Equal(world.HrId, returnStep.FromDepartmentId);
        Assert.Equal(world.DiwanId, returnStep.ToDepartmentId);
        Assert.Equal(MovementStepStatus.SentId, returnStep.StepStatusId);
    }

    [Fact]
    public async Task RejectMovement_ByTheCentralDiwan_StaysRejectedWithoutASelfStep()
    {
        var world = new World();
        await RegisterDocument(world);

        var movementResult = await world.CreateMovementHandler().Handle(
            new CreateMovementCommand(world.Document.DocumentId, world.HROfficer.UserId, world.DiwanId),
            CancellationToken.None);
        Assert.True(movementResult.IsSuccess, movementResult.Error?.Code);

        await world.SendMovementHandler().Handle(
            new SendMovementCommand(movementResult.Value.MovementId, world.HROfficer.UserId),
            CancellationToken.None);

        var result = await world.RejectHandler().Handle(
            new RejectMovementStepCommand(
                movementResult.Value.MovementId, world.Clerk.UserId, "غير مطابق"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(MovementStatus.RejectedId, result.Value.StatusId);
        Assert.Equal(1, result.Value.CurrentStepNo);
        Assert.Single(result.Value.Steps);
    }

    [Fact]
    public async Task ReturnMovement_TransitionsToReturned()
    {
        var world = new World();
        await RegisterDocument(world);
        var movement = await CreateMovement(world);
        await world.SendMovementHandler().Handle(
            new SendMovementCommand(movement.MovementId, world.Clerk.UserId),
            CancellationToken.None);

        // Receive first so return is allowed by domain rules.
        var registryNumber = DocumentNumber.CreateDepartmentEntry(
            world.Document.DocumentId,
            world.HrId,
            world.Registries.Registries.Single(r => r.DepartmentId == world.HrId).RegistryId,
            "HR-78",
            DateTime.UtcNow,
            world.HROfficer.UserId);
        Assert.True(registryNumber.IsSuccess);
        world.Numbers.Add(registryNumber.Value);
        world.Document.AddDepartmentNumber(registryNumber.Value);
        await world.ReceiveHandler().Handle(
            new ReceiveDocumentCommand(movement.MovementId, world.HROfficer.UserId),
            CancellationToken.None);

        var result = await world.ReturnHandler().Handle(
            new ReturnMovementToSenderCommand(movement.MovementId, world.HROfficer.UserId, "يحتاج تصحيح"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MovementStatus.ReturnedId, result.Value.StatusId);
    }

    [Fact]
    public async Task RedirectMovement_AppendsNewStepAndResends()
    {
        var world = new World();
        await RegisterDocument(world);
        var movement = await CreateMovement(world);
        await world.SendMovementHandler().Handle(
            new SendMovementCommand(movement.MovementId, world.Clerk.UserId),
            CancellationToken.None);

        // The document is at HR; the only legal next hop without another ordinary department is
        // back through the Central Diwan.
        var result = await world.RedirectHandler().Handle(
            new RedirectMovementCommand(
                movement.MovementId, world.Clerk.UserId, world.DiwanId, null, "تحويل"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(MovementStatus.SentId, result.Value.StatusId);
        Assert.Equal(2, result.Value.CurrentStepNo);
        Assert.Equal(2, result.Value.Steps.Count);
    }

    [Fact]
    public async Task CancelMovement_CancelsAndReleasesDocument()
    {
        var world = new World();
        await RegisterDocument(world);
        var movement = await CreateMovement(world);

        var result = await world.CancelHandler().Handle(
            new CancelMovementCommand(movement.MovementId, world.Clerk.UserId, "ملغى"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(MovementStatus.CanceledId, result.Value.StatusId);
        Assert.Equal(DocumentStatusType.RegisteredId, world.Document.CurrentStatusId);
    }

    [Fact]
    public async Task GetMovementById_And_ListByDocument()
    {
        var world = new World();
        await RegisterDocument(world);
        var movement = await CreateMovement(world);

        var get = await world.GetByIdHandler().Handle(
            new GetMovementByIdQuery(movement.MovementId), CancellationToken.None);
        Assert.True(get.IsSuccess);

        var list = await world.ListByDocumentHandler().Handle(
            new ListDocumentMovementsQuery(world.Document.DocumentId), CancellationToken.None);
        Assert.True(list.IsSuccess);
        Assert.Single(list.Value);
    }

    [Fact]
    public async Task Inbox_ListsTheDocumentWaitingForTheDestinationDepartment()
    {
        var world = new World();
        await RegisterDocument(world);
        var movement = await CreateMovement(world);
        await world.SendMovementHandler().Handle(
            new SendMovementCommand(movement.MovementId, world.Clerk.UserId),
            CancellationToken.None);

        var inbox = await world.InboxHandler().Handle(
            new GetDepartmentInboxQuery(world.HROfficer.UserId), CancellationToken.None);

        Assert.True(inbox.IsSuccess);
        var row = Assert.Single(inbox.Value);
        Assert.Equal(movement.MovementId, row.MovementId);
        Assert.Equal(world.Document.DocumentId, row.DocumentId);
        Assert.Equal(world.Document.Title, row.DocumentTitle);
        Assert.Equal(world.HrId, row.ToDepartmentId);
        Assert.Equal(MovementStepStatus.SentId, row.StepStatusId);
    }

    [Fact]
    public async Task Inbox_IsEmptyBeforeTheMovementIsSent()
    {
        var world = new World();
        await RegisterDocument(world);
        await CreateMovement(world);

        var inbox = await world.InboxHandler().Handle(
            new GetDepartmentInboxQuery(world.HROfficer.UserId), CancellationToken.None);

        Assert.True(inbox.IsSuccess);
        Assert.Empty(inbox.Value);
    }

    [Fact]
    public async Task Outbox_ListsTheDocumentTheDepartmentStillHasOut()
    {
        var world = new World();
        await RegisterDocument(world);
        var movement = await CreateMovement(world);
        await world.SendMovementHandler().Handle(
            new SendMovementCommand(movement.MovementId, world.Clerk.UserId),
            CancellationToken.None);

        var outbox = await world.OutboxHandler().Handle(
            new GetDepartmentOutboxQuery(world.Clerk.UserId), CancellationToken.None);

        Assert.True(outbox.IsSuccess);
        var row = Assert.Single(outbox.Value);
        Assert.Equal(movement.MovementId, row.MovementId);
        Assert.Equal(world.DiwanId, row.FromDepartmentId);
        Assert.Equal(world.HrId, row.ToDepartmentId);
    }

    [Fact]
    public async Task Inbox_ForADepartmentOutsideTheCallersScope_IsForbidden()
    {
        var world = new World();
        await RegisterDocument(world);
        await CreateMovement(world);

        var result = await world.InboxHandler().Handle(
            new GetDepartmentInboxQuery(world.Clerk.UserId, world.HrId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Movements.DepartmentOutOfScope.Code, result.Error.Code);
    }

    [Fact]
    public async Task Inbox_ForADepartmentInsideTheCallersScope_IsAllowed()
    {
        var world = new World();
        await RegisterDocument(world);
        var movement = await CreateMovement(world);
        await world.SendMovementHandler().Handle(
            new SendMovementCommand(movement.MovementId, world.Clerk.UserId),
            CancellationToken.None);
        world.Scope.AccessibleDepartments.Add(world.HrId);

        var result = await world.InboxHandler().Handle(
            new GetDepartmentInboxQuery(world.Clerk.UserId, world.HrId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }
}
