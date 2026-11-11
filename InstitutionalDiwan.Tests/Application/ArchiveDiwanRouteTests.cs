using InstitutionalDiwan.Application.Features.Archiving;
using InstitutionalDiwan.Application.Features.Correspondence.Documents;
using InstitutionalDiwan.Application.Features.DepartmentRecords;
using InstitutionalDiwan.Application.Features.Movements;
using InstitutionalDiwan.Application.Features.Numbering.Numbers;
using InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>
/// The route to the archive diwan: any department sends a document for archiving to the department
/// that created it, only that department sends it on to the archive diwan, and the archive diwan is
/// where the document is archived and reopened.
/// </summary>
public sealed class ArchiveDiwanRouteTests
{
    private const int Location = ArchiveLocation.PhysicalArchiveId;

    private sealed class World
    {
        public FakeUserRepository Users { get; } = new();
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentNumberRepository Numbers { get; } = new();
        public FakeNumberingRegistryRepository Registries { get; } = new();
        public FakeDepartmentRepository Departments { get; } = new();
        public FakeInstitutionSettingRepository Settings { get; } = new();
        public FakeDocumentMovementRepository Movements { get; } = new();
        public FakeDocumentArchiveRepository Archive { get; } = new();
        public FakeLookupRepository Lookups { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();

        /// <summary>The Central Diwan; it created the document.</summary>
        public Guid CentralId { get; }
        public Guid IndividualId { get; }
        public Guid FinanceId { get; }
        public Guid ArchiveId { get; }
        public User Clerk { get; }
        public User Judge { get; }
        public User Archivist { get; }
        public InstitutionSetting Setting { get; }
        public Document Document { get; }

        public World(bool designateArchiveDiwan = true)
        {
            CentralId = AddDepartment("CENTRAL");
            IndividualId = AddDepartment("INDIVIDUAL");
            FinanceId = AddDepartment("FINANCE");
            ArchiveId = AddDepartment("ARCHIVE");

            Setting = InstitutionSetting.Create("المؤسسة", 1, 1, centralDiwanDepartmentId: CentralId).Value;
            if (designateArchiveDiwan)
                Assert.True(Setting.SetArchiveDiwan(ArchiveId).IsSuccess);
            Settings.Add(Setting);

            Clerk = AddUser("clerk", CentralId);
            Judge = AddUser("judge", IndividualId);
            Archivist = AddUser("archivist", ArchiveId);

            Document = NewDocument(CentralId, Clerk);
            Lookups.Add("archive-locations", ArchiveLocation.Create(Location, "Physical", "ورقي").Value);
        }

        public Guid AddDepartment(string code)
        {
            var department = Department.Create(code, $"دائرة {code}", 1).Value;
            Departments.Add(department);
            Registries.Add(NumberingRegistry.Create(code, $"سجل {code}", departmentId: department.DepartmentId).Value);
            return department.DepartmentId;
        }

        private User AddUser(string username, Guid departmentId)
        {
            var user = User.Create(username, "hash", departmentId).Value;
            Users.Add(user);
            return user;
        }

        public Document NewDocument(Guid owningDepartmentId, User author)
        {
            var document = Document.CreateDraft(
                "إضبارة", DocumentType.InternalMemoId, DocumentDirection.InternalId,
                owningDepartmentId, author.UserId).Value;
            Documents.Add(document);
            return document;
        }

        public async Task RegisterAsync(Document? document = null)
        {
            document ??= Document;
            var result = await new RegisterDocumentCommandHandler(
                    Documents, Numbers, Registries, new FakeDocumentTypeSchemaVersionRepository(),
                    new FakeDocumentAccessEvaluator { AllowAll = true }, UnitOfWork, FakeNotificationPublisher.Instance, new FakeSender())
                .Handle(new RegisterDocumentCommand(document.DocumentId, Clerk.UserId, 2026), CancellationToken.None);
            Assert.True(result.IsSuccess, result.Error?.Code);
        }

        public static FakeDepartmentScopeEvaluator ScopeOf(Guid departmentId)
        {
            var scope = new FakeDepartmentScopeEvaluator { PrimaryDepartmentId = departmentId };
            scope.AccessibleDepartments.Add(departmentId);
            return scope;
        }

        public CentralDiwanRouting Routing() => new(Settings);

        public ArchiveDiwanRouting ArchiveRouting() => new(Settings, Documents);

        public DepartmentRecordService Records() => FakeDepartmentRecords.Service(Documents);

        public Task<Result<MovementResponse>> OpenAsync(User sender, Guid destinationId, int movementTypeId = MovementType.ReferralId) =>
            new CreateMovementCommandHandler(
                    Movements, Documents, Departments, Users, Routing(), ArchiveRouting(),
                    new FakeDocumentAccessEvaluator { AllowAll = true }, UnitOfWork)
                .Handle(new CreateMovementCommand(Document.DocumentId, sender.UserId, destinationId, movementTypeId), CancellationToken.None);

        public Task<Result<MovementResponse>> SendAsync(User sender, Guid movementId) =>
            new SendMovementCommandHandler(
                    Movements, Routing(), ArchiveRouting(), ScopeOf(sender.PrimaryDepartmentId), UnitOfWork,
                    FakeNotificationPublisher.Instance, Records())
                .Handle(new SendMovementCommand(movementId, sender.UserId), CancellationToken.None);

        public async Task ReceiveAsync(User receiver, Guid movementId, string registryNumber)
        {
            var scope = ScopeOf(receiver.PrimaryDepartmentId);
            var result = await new ReceiveDocumentCommandHandler(
                    Movements, Documents, scope, UnitOfWork, FakeNotificationPublisher.Instance,
                    new DepartmentRegistrationService(Numbers, Departments, Registries, scope, UnitOfWork),
                    TestClock.Instance, Records())
                .Handle(new ReceiveDocumentCommand(movementId, receiver.UserId, RegistryNumberText: registryNumber), CancellationToken.None);
            Assert.True(result.IsSuccess, result.Error?.Code);
        }

        public Task<Result<MovementResponse>> RedirectAsync(User sender, Guid movementId, Guid destinationId, int? movementTypeId = null) =>
            new RedirectMovementCommandHandler(
                    Movements, Departments, Users, Routing(), ArchiveRouting(), ScopeOf(sender.PrimaryDepartmentId),
                    UnitOfWork, FakeNotificationPublisher.Instance, Records())
                .Handle(
                    new RedirectMovementCommand(movementId, sender.UserId, destinationId, MovementTypeId: movementTypeId),
                    CancellationToken.None);

        /// <summary>
        /// No document-level access at all: with an archive diwan designated, custody decides.
        /// </summary>
        public Task<Result<DocumentResponse>> ArchiveAsync(User actor) =>
            new ArchiveDocumentCommandHandler(
                    Documents, Movements, Archive, Lookups, new FakeDocumentAccessEvaluator(), UnitOfWork,
                    FakeNotificationPublisher.Instance, ScopeOf(actor.PrimaryDepartmentId), Records(), Settings)
                .Handle(new ArchiveDocumentCommand(Document.DocumentId, actor.UserId, Location), CancellationToken.None);

        public Task<Result<DocumentResponse>> UnarchiveAsync(User actor, string reason = "ظهور دليل جديد") =>
            new UnarchiveDocumentCommandHandler(
                    Documents, Archive, new FakeDocumentAccessEvaluator(), UnitOfWork,
                    ScopeOf(actor.PrimaryDepartmentId), Settings, Movements,
                    new FakeDocumentDecisionRepository())
                .Handle(new UnarchiveDocumentCommand(Document.DocumentId, actor.UserId, reason), CancellationToken.None);

        /// <summary>
        /// Central Diwan → the judge's department → back to the Central Diwan for archiving → the
        /// archive diwan, which receives it. Returns the one movement that carried it all the way.
        /// </summary>
        public async Task<Guid> BringToArchiveDiwanAsync()
        {
            await RegisterAsync();

            var opened = await OpenAsync(Clerk, IndividualId);
            Assert.True(opened.IsSuccess, opened.Error?.Code);
            var movementId = opened.Value.MovementId;
            Assert.True((await SendAsync(Clerk, movementId)).IsSuccess);
            await ReceiveAsync(Judge, movementId, "IND-1");

            var toOwner = await RedirectAsync(Judge, movementId, CentralId, MovementType.ToArchiveId);
            Assert.True(toOwner.IsSuccess, toOwner.Error?.Code);
            await ReceiveAsync(Clerk, movementId, "CEN-1");

            var toArchive = await RedirectAsync(Clerk, movementId, ArchiveId);
            Assert.True(toArchive.IsSuccess, toArchive.Error?.Code);
            await ReceiveAsync(Archivist, movementId, "ARC-1");
            return movementId;
        }
    }

    [Fact]
    public async Task FullRoute_AnotherDiwanThroughTheOwnerToTheArchiveDiwan_ArchivesAndClosesTheMovement()
    {
        var world = new World();
        var movementId = await world.BringToArchiveDiwanAsync();

        var archived = await world.ArchiveAsync(world.Archivist);

        Assert.True(archived.IsSuccess, archived.Error?.Code);
        Assert.Equal(DocumentStatusType.ArchivedId, world.Document.CurrentStatusId);
        var movement = world.Movements.Movements.Single(candidate => candidate.MovementId == movementId);
        Assert.Equal(MovementType.ToArchiveId, movement.MovementTypeId);
        Assert.Equal(MovementStatus.ClosedId, movement.StatusId);
        Assert.Equal(ArchiveEventType.ArchivedId, Assert.Single(world.Archive.Events).EventTypeId);
    }

    [Fact]
    public async Task Reopen_ByTheArchiveDiwan_RestoresTheDocumentAndItCanLeaveAgain()
    {
        var world = new World();
        await world.BringToArchiveDiwanAsync();
        Assert.True((await world.ArchiveAsync(world.Archivist)).IsSuccess);

        var reopened = await world.UnarchiveAsync(world.Archivist);

        Assert.True(reopened.IsSuccess, reopened.Error?.Code);
        Assert.Equal(DocumentStatusType.RegisteredId, world.Document.CurrentStatusId);

        var back = await world.OpenAsync(world.Archivist, world.CentralId);
        Assert.True(back.IsSuccess, back.Error?.Code);
    }

    [Fact]
    public async Task Reopen_ByTheOwningDepartment_IsRefusedWhileTheArchiveDiwanHoldsTheDocument()
    {
        var world = new World();
        await world.BringToArchiveDiwanAsync();
        Assert.True((await world.ArchiveAsync(world.Archivist)).IsSuccess);

        var result = await world.UnarchiveAsync(world.Clerk);

        Assert.Equal(DomainErrors.Archiving.OnlyArchiveDiwanCanArchive.Code, result.Error.Code);
        Assert.Equal(DocumentStatusType.ArchivedId, world.Document.CurrentStatusId);
    }

    [Fact]
    public async Task Archive_ByTheOwningDepartmentWhereTheDocumentWasCreated_IsRefusedOnceAnArchiveDiwanExists()
    {
        var world = new World();
        await world.RegisterAsync();

        var result = await world.ArchiveAsync(world.Clerk);

        Assert.Equal(DomainErrors.Archiving.DocumentNotAtArchiveDiwan.Code, result.Error.Code);
        Assert.Equal(DocumentStatusType.RegisteredId, world.Document.CurrentStatusId);
    }

    [Fact]
    public async Task Archive_AtTheArchiveDiwanByAnotherDepartment_IsForbidden()
    {
        var world = new World();
        await world.BringToArchiveDiwanAsync();

        var result = await world.ArchiveAsync(world.Clerk);

        Assert.Equal(DomainErrors.Archiving.OnlyArchiveDiwanCanArchive.Code, result.Error.Code);
        Assert.Equal(ErrorType.Forbidden, result.Error.Type);
    }

    [Fact]
    public async Task OrdinaryMovement_ToTheArchiveDiwan_IsRefused()
    {
        var world = new World();
        await world.RegisterAsync();

        var result = await world.OpenAsync(world.Clerk, world.ArchiveId);

        Assert.Equal(DomainErrors.Archiving.ArchiveDiwanAcceptsArchiveMovementsOnly.Code, result.Error.Code);
    }

    [Fact]
    public async Task ArchiveMovement_FromTheOwnerStraightToTheArchiveDiwan_IsAllowed()
    {
        var world = new World();
        await world.RegisterAsync();

        var opened = await world.OpenAsync(world.Clerk, world.ArchiveId, MovementType.ToArchiveId);

        Assert.True(opened.IsSuccess, opened.Error?.Code);
        Assert.True((await world.SendAsync(world.Clerk, opened.Value.MovementId)).IsSuccess);
    }

    [Fact]
    public async Task ArchiveMovement_RedirectedByTheOwnerToAnOrdinaryDepartment_NeedsAnOrdinaryType()
    {
        var world = new World();
        await world.RegisterAsync();
        var movementId = (await world.OpenAsync(world.Clerk, world.IndividualId)).Value.MovementId;
        Assert.True((await world.SendAsync(world.Clerk, movementId)).IsSuccess);
        await world.ReceiveAsync(world.Judge, movementId, "IND-1");
        Assert.True((await world.RedirectAsync(world.Judge, movementId, world.CentralId, MovementType.ToArchiveId)).IsSuccess);
        await world.ReceiveAsync(world.Clerk, movementId, "CEN-1");

        var stillForArchive = await world.RedirectAsync(world.Clerk, movementId, world.FinanceId);
        Assert.Equal(DomainErrors.Archiving.ArchiveRouteViaOwningDepartment.Code, stillForArchive.Error.Code);

        var notArchivedAfterAll = await world.RedirectAsync(world.Clerk, movementId, world.FinanceId, MovementType.ReferralId);
        Assert.True(notArchivedAfterAll.IsSuccess, notArchivedAfterAll.Error?.Code);
        Assert.Equal(MovementType.ReferralId, notArchivedAfterAll.Value.MovementTypeId);
    }

    [Fact]
    public async Task Routing_ArchiveMovementFromAnotherDepartmentToTheArchiveDiwan_IsRefused()
    {
        var world = new World();

        var result = await world.ArchiveRouting().ValidateFirstHopAsync(
            world.Document.DocumentId, MovementType.ToArchiveId, world.IndividualId, world.ArchiveId, CancellationToken.None);

        Assert.Equal(DomainErrors.Archiving.OnlyOwningDepartmentSendsToArchive.Code, result.Error.Code);
    }

    [Fact]
    public async Task Routing_ArchiveMovementWithoutAnArchiveDiwan_IsRefused_AndOrdinaryMovementsAreUntouched()
    {
        var world = new World(designateArchiveDiwan: false);
        var routing = world.ArchiveRouting();

        var forArchive = await routing.ValidateFirstHopAsync(
            world.Document.DocumentId, MovementType.ToArchiveId, world.IndividualId, world.CentralId, CancellationToken.None);
        var ordinary = await routing.ValidateFirstHopAsync(
            world.Document.DocumentId, MovementType.ReferralId, world.CentralId, world.ArchiveId, CancellationToken.None);

        Assert.Equal(DomainErrors.Archiving.ArchiveDiwanNotConfigured.Code, forArchive.Error.Code);
        Assert.True(ordinary.IsSuccess);
    }

    [Fact]
    public async Task Routing_OwnerThatIsNotTheCentralDiwan_ReachesTheArchiveDiwanThroughTheCentralDiwanOnly()
    {
        var world = new World();
        var owned = world.NewDocument(world.IndividualId, world.Judge);
        var routing = world.ArchiveRouting();

        var viaOwner = DocumentMovement.Create(
            owned.DocumentId, world.IndividualId, world.CentralId, world.Judge.UserId, MovementType.ToArchiveId).Value;
        var relayed = await routing.ValidateNextHopAsync(
            viaOwner, world.ArchiveId, MovementType.ToArchiveId, CancellationToken.None);

        var skippingOwner = DocumentMovement.Create(
            owned.DocumentId, world.FinanceId, world.CentralId, world.Clerk.UserId, MovementType.ToArchiveId).Value;
        var bypass = await routing.ValidateNextHopAsync(
            skippingOwner, world.ArchiveId, MovementType.ToArchiveId, CancellationToken.None);

        Assert.True(relayed.IsSuccess, relayed.Error?.Code);
        Assert.Equal(DomainErrors.Archiving.OnlyOwningDepartmentSendsToArchive.Code, bypass.Error.Code);
    }

    [Fact]
    public async Task WithoutAnArchiveDiwan_TheOwningDepartmentStillArchivesWhereTheDocumentIs()
    {
        var world = new World(designateArchiveDiwan: false);
        await world.RegisterAsync();
        var access = new FakeDocumentAccessEvaluator();
        access.ManagedDocuments.Add(world.Document.DocumentId);

        var result = await new ArchiveDocumentCommandHandler(
                world.Documents, world.Movements, world.Archive, world.Lookups, access, world.UnitOfWork,
                FakeNotificationPublisher.Instance, World.ScopeOf(world.CentralId), world.Records(), world.Settings)
            .Handle(new ArchiveDocumentCommand(world.Document.DocumentId, world.Clerk.UserId, Location), CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(DocumentStatusType.ArchivedId, world.Document.CurrentStatusId);
    }

    [Fact]
    public void Setting_ArchiveDiwanCannotBeTheCentralDiwan()
    {
        var world = new World(designateArchiveDiwan: false);

        var result = world.Setting.SetArchiveDiwan(world.CentralId);

        Assert.Equal(DomainErrors.InstitutionSetting.ArchiveDiwanSameAsCentral.Code, result.Error.Code);
        Assert.Null(world.Setting.ArchiveDiwanDepartmentId);
    }

    [Fact]
    public async Task Settings_DesignatingAnUnknownOrInactiveArchiveDiwan_IsRefused_AndNullClearsIt()
    {
        var central = Department.Create("CENTRAL", "الديوان المركزي", DepartmentType.CentralDiwanId).Value;
        var archive = Department.Create("ARCHIVE", "ديوان الحفظ", DepartmentType.SectionId).Value;
        var closed = Department.Create("CLOSED", "قسم موقوف", DepartmentType.SectionId).Value;
        closed.Deactivate(hasActiveChildren: false);
        var departments = new FakeDepartmentRepository();
        departments.Add(central);
        departments.Add(archive);
        departments.Add(closed);

        var settings = new FakeInstitutionSettingRepository();
        settings.Add(InstitutionSetting.Create(
            "المؤسسة", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId,
            centralDiwanDepartmentId: central.DepartmentId).Value);
        var lookups = new FakeLookupRepository();
        lookups.Add("numbering-modes", NumberingMode.Create(NumberingMode.AutomaticId, "Automatic", "تلقائي").Value);
        lookups.Add("numbering-reset-policies", NumberingResetPolicy.Create(NumberingResetPolicy.YearlyId, "Yearly", "سنوي").Value);

        Task<Result<InstitutionSettingResponse>> UpdateAsync(Guid? archiveDiwanDepartmentId) =>
            new UpdateInstitutionSettingsCommandHandler(
                    settings, departments, new FakeEmployeeRepository(), lookups, new FakeUnitOfWork())
                .Handle(new UpdateInstitutionSettingsCommand(
                    "المؤسسة", NumberingMode.AutomaticId, NumberingResetPolicy.YearlyId, central.DepartmentId,
                    ArchiveDiwanDepartmentId: archiveDiwanDepartmentId), CancellationToken.None);

        Assert.Equal(DomainErrors.InstitutionSetting.ArchiveDiwanNotFound.Code, (await UpdateAsync(Guid.NewGuid())).Error.Code);
        Assert.Equal(DomainErrors.InstitutionSetting.ArchiveDiwanInactive.Code, (await UpdateAsync(closed.DepartmentId)).Error.Code);
        Assert.Equal(DomainErrors.InstitutionSetting.ArchiveDiwanSameAsCentral.Code, (await UpdateAsync(central.DepartmentId)).Error.Code);

        var designated = await UpdateAsync(archive.DepartmentId);
        Assert.True(designated.IsSuccess, designated.Error?.Code);
        Assert.Equal(archive.DepartmentId, designated.Value.ArchiveDiwanDepartmentId);

        var cleared = await UpdateAsync(null);
        Assert.True(cleared.IsSuccess, cleared.Error?.Code);
        Assert.Null(cleared.Value.ArchiveDiwanDepartmentId);
    }
}
