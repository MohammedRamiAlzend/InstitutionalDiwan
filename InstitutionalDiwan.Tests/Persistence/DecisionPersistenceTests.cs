using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Decisions;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// The decision register hides documents the caller cannot view, and the database keeps one
/// in-force decision per document and one number per department and year.
/// </summary>
public sealed class DecisionPersistenceTests
{
    private static readonly DateTime Now = TestClock.Instance.UtcNow;

    private sealed record World(
        InstitutionalDiwan.Infrastructure.Persistence.DiwanDbContext Db,
        Department Home,
        Department Away,
        NumberingRegistry Book,
        User Caller,
        User Stranger);

    private static async Task<World> CreateWorldAsync()
    {
        var db = TestContextFactory.Create();
        db.DepartmentTypes.Add(DepartmentType.Create(DepartmentType.SectionId, "Section", "شعبة", 0).Value);
        db.UserScopeTypes.Add(UserScopeType.Create(UserScopeType.DepartmentId, "Department", "قسم").Value);
        db.DocumentDirections.Add(DocumentDirection.Create(DocumentDirection.InternalId, "Internal", "داخلي").Value);
        db.DocumentTypes.Add(DocumentType.Create(DocumentType.InternalMemoId, "Memo", "مذكرة", DocumentDirection.InternalId).Value);
        db.DocumentStatusTypes.Add(DocumentStatusType.Create(DocumentStatusType.DraftId, "Draft", "مسودة").Value);
        db.NumberingResetPolicies.Add(NumberingResetPolicy.Create(NumberingResetPolicy.YearlyId, "Yearly", "سنوي").Value);
        db.DocumentNumberScopes.Add(DocumentNumberScope.Create(DocumentNumberScope.DepartmentId, "Department", "قسم").Value);
        db.DocumentNumberStatuses.Add(DocumentNumberStatus.Create(DocumentNumberStatus.ActiveId, "Active", "فعال").Value);
        db.DocumentNumberEventTypes.Add(DocumentNumberEventType.Create(
            DocumentNumberEventType.RegisteredInDepartmentId, "Registered", "تسجيل").Value);

        var home = Department.Create("HOME", "الديوان", DepartmentType.SectionId).Value;
        var away = Department.Create("AWAY", "البعيد", DepartmentType.SectionId).Value;
        db.Departments.AddRange(home, away);
        var caller = User.Create("caller", "hash", home.DepartmentId).Value;
        var stranger = User.Create("stranger", "hash", away.DepartmentId).Value;
        db.Users.AddRange(caller, stranger);
        var book = NumberingRegistry.Create("HOME-BOOK", "دفتر", departmentId: home.DepartmentId).Value;
        db.NumberingRegistries.Add(book);
        await db.SaveChangesAsync();
        return new World(db, home, away, book, caller, stranger);
    }

    private static async Task<DocumentDecision> AddDecisionAsync(
        World world, Guid ownerId, Guid authorId, string number, int statusId = DecisionStatus.FinalId, bool appealable = false)
    {
        var document = Document.CreateDraft(
            null, DocumentType.InternalMemoId, DocumentDirection.InternalId, ownerId, authorId,
            utcNow: Now, displayName: number).Value;
        var line = DocumentNumber.CreateDepartmentEntry(
            document.DocumentId, ownerId, world.Book.RegistryId, number, new DateTime(2026, 3, 9), authorId, utcNow: Now).Value;
        Assert.True(document.AddDepartmentNumber(line).IsSuccess);
        world.Db.Documents.Add(document);

        var type = DecisionType.Create(
            appealable ? "الظن" : "منع المحاكمة",
            appealable,
            appealable ? DecisionEffect.NoneId : DecisionEffect.SendToArchiveId,
            null,
            [DocumentType.InternalMemoId]).Value;
        world.Db.DecisionTypes.Add(type);
        await world.Db.SaveChangesAsync();

        var visitId = line.Visits.Single().VisitId;
        var decision = DocumentDecision.Issue(
            document.DocumentId, document.DocumentTypeId, type, number, new DateOnly(2026, 11, 11),
            ownerId, visitId, null, null, authorId, false, false, Now).Value;
        if (statusId == DecisionStatus.CanceledId)
            Assert.True(decision.Cancel(authorId, "إلغاء", Now).IsSuccess);
        world.Db.DocumentDecisions.Add(decision);
        await world.Db.SaveChangesAsync();
        return decision;
    }

    [Fact]
    public async Task Register_HidesDecisionsOnDocumentsTheCallerCannotView()
    {
        var world = await CreateWorldAsync();
        var visible = await AddDecisionAsync(world, world.Home.DepartmentId, world.Caller.UserId, "1/2026");
        await AddDecisionAsync(world, world.Away.DepartmentId, world.Stranger.UserId, "2/2026");

        var page = await new DocumentDecisionRepository(world.Db).ListPagedAsync(
            new PageRequest(1, 20, null, null, true),
            new DocumentViewAccess(world.Caller.UserId, new[] { world.Home.DepartmentId }, Now),
            new DecisionRegisterFilter(null, null, null, null));

        Assert.Equal(2, await world.Db.DocumentDecisions.CountAsync());
        var row = Assert.Single(page.Items);
        Assert.Equal(visible.DecisionId, row.Decision.DecisionId);
    }

    [Fact]
    public async Task Database_AllowsOneInForceDecision_AndKeepsACanceledNumberTaken()
    {
        var world = await CreateWorldAsync();
        var first = await AddDecisionAsync(world, world.Home.DepartmentId, world.Caller.UserId, "7/2026");

        var document = await world.Db.Documents.SingleAsync(item => item.DocumentId == first.DocumentId);
        var visitId = first.RegistryVisitId;
        var type = await world.Db.DecisionTypes.SingleAsync();
        var duplicate = DocumentDecision.Issue(
            document.DocumentId, document.DocumentTypeId, type, "8/2026", new DateOnly(2026, 11, 11),
            world.Home.DepartmentId, visitId, null, null, world.Caller.UserId, false, false, Now).Value;
        world.Db.DocumentDecisions.Add(duplicate);

        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
        world.Db.ChangeTracker.Clear();

        var stored = await world.Db.DocumentDecisions.SingleAsync();
        Assert.True(stored.Cancel(world.Caller.UserId, "أُدخل خطأ", Now).IsSuccess);
        await world.Db.SaveChangesAsync();

        var sameNumber = DocumentDecision.Issue(
            document.DocumentId, document.DocumentTypeId, type, "7/2026", new DateOnly(2026, 11, 12),
            world.Home.DepartmentId, visitId, null, null, world.Caller.UserId, false, false, Now).Value;
        world.Db.DocumentDecisions.Add(sameNumber);
        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Appeal_RoundTripsWithTheDecision()
    {
        var world = await CreateWorldAsync();
        var decision = await AddDecisionAsync(
            world, world.Home.DepartmentId, world.Caller.UserId, "APP/2026", appealable: true);
        var repository = new DocumentDecisionRepository(world.Db);

        var tracked = await repository.GetByIdAsync(decision.DecisionId);
        Assert.NotNull(tracked);
        var recorded = tracked.RecordAppeal(
            new DateOnly(2026, 11, 11), world.Home.DepartmentId, "REF-1", "ملاحظة", world.Caller.UserId, Now);
        Assert.True(recorded.IsSuccess, recorded.Error.Code);
        await world.Db.SaveChangesAsync();

        var appealed = await repository.ListByDocumentAsync(decision.DocumentId);
        var stored = Assert.Single(appealed);
        Assert.Equal(DecisionStatus.AppealedId, stored.StatusId);
        var appeal = Assert.Single(stored.Appeals);
        Assert.Equal(AppealOutcome.PendingId, appeal.OutcomeId);
        Assert.Equal("REF-1", appeal.ReferenceNumber);

        world.Db.ChangeTracker.Clear();
        var again = await repository.GetByIdAsync(decision.DecisionId);
        Assert.NotNull(again);
        Assert.True(again.RecordAppealOutcome(
            appeal.AppealId, AppealOutcome.AcceptedId, new DateOnly(2026, 11, 12), world.Caller.UserId, Now).IsSuccess);
        await world.Db.SaveChangesAsync();

        world.Db.ChangeTracker.Clear();
        var revoked = Assert.Single(await repository.ListByDocumentAsync(decision.DocumentId));
        Assert.Equal(DecisionStatus.RevokedId, revoked.StatusId);
        Assert.Equal(AppealOutcome.AcceptedId, Assert.Single(revoked.Appeals).OutcomeId);
        Assert.Equal(new DateOnly(2026, 11, 12), revoked.Appeals.Single().OutcomeDate);
    }
}
