using InstitutionalDiwan.Application.Features.Relations;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Entities.Relations;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Interceptors;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Runs the Module 08 handlers against a real SQLite database with the real repositories, so link
/// rules, cycle detection, access filtering, constraints and audit rows are checked end to end.
/// </summary>
public sealed class RelationsTests
{
    private const int ReplyTo = DocumentRelationType.ReplyToId;
    private const int Related = DocumentRelationType.RelatedId;
    private const int Supersedes = DocumentRelationType.SupersedesId;
    private const int Retired = 99;

    private sealed class World
    {
        public required DiwanDbContext Db { get; init; }
        public required User Author { get; init; }
        public required User Stranger { get; init; }
        public required Department Home { get; init; }
        public required Department Away { get; init; }
        public FakeDocumentAccessEvaluator Access { get; } = new() { AllowAll = true };
        public FakeDepartmentScopeEvaluator Scope { get; } = new();
        public FakeUserContext Caller { get; } = new();

        public DocumentRepository Documents => new(Db);
        public DocumentRelationRepository Relations => new(Db);

        public Task<Result<DocumentRelationResponse>> LinkAsync(Document from, Document to, int type, string? notes = null) =>
            new CreateDocumentRelationCommandHandler(Documents, Relations, Access, Db)
                .Handle(new CreateDocumentRelationCommand(from.DocumentId, Author.UserId, to.DocumentId, type, notes), default);

        public Task<Result> UnlinkAsync(Document from, Guid relationId, string reason = "ربط خاطئ") =>
            new RemoveDocumentRelationCommandHandler(Documents, Relations, Access, Db)
                .Handle(new RemoveDocumentRelationCommand(from.DocumentId, relationId, Author.UserId, reason), default);

        public Task<Result<DocumentRelationsResponse>> ListAsync(Document document) =>
            new ListDocumentRelationsQueryHandler(Documents, Relations, Access, Scope, Caller, TestClock.Instance)
                .Handle(new ListDocumentRelationsQuery(document.DocumentId), default);

        public Task<Result<RelationGraphResponse>> GraphAsync(Document root, int depth = 2) =>
            new GetDocumentRelationsGraphQueryHandler(Documents, Relations, Access, Scope, Caller, TestClock.Instance)
                .Handle(new GetDocumentRelationsGraphQuery(root.DocumentId, depth), default);

        public async Task<Document> DocumentAsync(string title = "كتاب", User? by = null)
        {
            var user = by ?? Author;
            var document = Document.CreateDraft(
                title, DocumentType.InternalMemoId, DocumentDirection.InternalId,
                user.PrimaryDepartmentId, user.UserId).Value;
            Db.Documents.Add(document);
            await Db.SaveChangesAsync();
            return document;
        }

        public async Task<Document> LinkedAsync(Document from, int type = ReplyTo, string title = "مرتبط")
        {
            var to = await DocumentAsync(title);
            var result = await LinkAsync(from, to, type);
            Assert.True(result.IsSuccess, result.Error?.Code);
            return to;
        }
    }

    private static async Task<World> CreateWorldAsync(DiwanDbContext? existing = null)
    {
        var db = existing ?? TestContextFactory.Create();

        db.DepartmentTypes.Add(DepartmentType.Create(DepartmentType.SectionId, "Section", "شعبة", 0).Value);
        foreach (var id in new[] { UserScopeType.DepartmentId, UserScopeType.DepartmentTreeId, UserScopeType.InstitutionId, UserScopeType.SystemId })
            db.UserScopeTypes.Add(UserScopeType.Create(id, $"Scope {id}", $"نطاق {id}").Value);
        foreach (var id in new[] { AccessLevel.ViewId, AccessLevel.DownloadId, AccessLevel.ForwardId, AccessLevel.ManageId })
            db.AccessLevels.Add(AccessLevel.Create(id, $"Level {id}", $"مستوى {id}").Value);
        db.AssignmentTypes.Add(AssignmentType.Create(AssignmentType.AdditionalId, "Additional", "إضافي").Value);
        db.DocumentDirections.Add(DocumentDirection.Create(DocumentDirection.InternalId, "Internal", "داخلي").Value);
        db.DocumentTypes.Add(DocumentType.Create(DocumentType.InternalMemoId, "Memo", "مذكرة", DocumentDirection.InternalId).Value);
        db.DocumentPriorityLevels.Add(DocumentPriorityLevel.Create(DocumentPriorityLevel.NormalId, "Normal", "عادية").Value);
        foreach (var id in new[] { DocumentConfidentialityLevel.NormalId, DocumentConfidentialityLevel.SecretId, DocumentConfidentialityLevel.TopSecretId })
            db.DocumentConfidentialityLevels.Add(DocumentConfidentialityLevel.Create(id, $"Level {id}", $"مستوى {id}").Value);
        foreach (var id in new[] { DocumentStatusType.DraftId, DocumentStatusType.RegisteredId, DocumentStatusType.ArchivedId, DocumentStatusType.CanceledId })
            db.DocumentStatusTypes.Add(DocumentStatusType.Create(id, $"Status {id}", $"حالة {id}").Value);
        db.ArchiveEventTypes.Add(ArchiveEventType.Create(ArchiveEventType.ArchivedId, "Archived", "أرشفة").Value);
        db.ArchiveLocations.Add(ArchiveLocation.Create(ArchiveLocation.PhysicalArchiveId, "Physical", "ورقي").Value);

        db.DocumentRelationTypes.Add(DocumentRelationType.Create(ReplyTo, "Reply to", "ردّ على").Value);
        db.DocumentRelationTypes.Add(DocumentRelationType.Create(Related, "Related", "مرتبط", isSymmetric: true, preventCycles: false).Value);
        db.DocumentRelationTypes.Add(DocumentRelationType.Create(Supersedes, "Supersedes", "يلغي").Value);
        db.DocumentRelationTypes.Add(DocumentRelationType.Create(
            DocumentRelationType.JoinedIntoId, "Joined into", "موحَّد مع").Value);
        var retired = DocumentRelationType.Create(Retired, "Retired", "متقاعد").Value;
        retired.Deactivate();
        db.DocumentRelationTypes.Add(retired);
        foreach (var (id, name) in new[] { (RelationActionType.CreatedId, "Created"), (RelationActionType.UpdatedId, "Updated"), (RelationActionType.RemovedId, "Removed") })
            db.RelationActionTypes.Add(RelationActionType.Create(id, name, name).Value);

        var home = Department.Create("HOME", "الرئيسي", DepartmentType.SectionId).Value;
        var away = Department.Create("AWAY", "البعيد", DepartmentType.SectionId).Value;
        db.Departments.AddRange(home, away);
        var author = User.Create("author", "hash", home.DepartmentId).Value;
        var stranger = User.Create("stranger", "hash", away.DepartmentId).Value;
        db.Users.AddRange(author, stranger);
        await db.SaveChangesAsync();

        var world = new World { Db = db, Author = author, Stranger = stranger, Home = home, Away = away };
        world.Caller.UserId = author.UserId;
        world.Scope.AccessibleDepartments.Add(home.DepartmentId);
        return world;
    }

    private static void AssertError(Error expected, Result result)
    {
        Assert.True(result.IsFailure);
        Assert.Equal(expected.Code, result.Error.Code);
    }

    // ---- Create --------------------------------------------------------------------------------

    [Fact]
    public async Task Create_LinksDocumentsAndRecordsTheEventInTheSameSave()
    {
        var world = await CreateWorldAsync();
        var reply = await world.DocumentAsync("الرد");
        var request = await world.DocumentAsync("الطلب");

        var result = await world.LinkAsync(reply, request, ReplyTo, " ردًا على الطلب ");

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(RelationDirection.Outgoing, result.Value.Direction);
        Assert.Equal(request.DocumentId, result.Value.RelatedDocument.DocumentId);
        Assert.Equal("ردًا على الطلب", result.Value.Notes);

        var relation = await world.Db.DocumentRelations.SingleAsync();
        Assert.Equal((reply.DocumentId, request.DocumentId), (relation.SourceDocumentId, relation.TargetDocumentId));
        var relationEvent = await world.Db.DocumentRelationEvents.SingleAsync();
        Assert.Equal(relation.RelationId, relationEvent.RelationId);
        Assert.Equal(RelationActionType.CreatedId, relationEvent.ActionTypeId);
    }

    [Fact]
    public async Task Create_SymmetricLink_IsStoredOnceWhicheverWayItIsRequested()
    {
        var world = await CreateWorldAsync();
        var first = await world.DocumentAsync("أول");
        var second = await world.DocumentAsync("ثان");

        Assert.True((await world.LinkAsync(first, second, Related)).IsSuccess);
        var reversed = await world.LinkAsync(second, first, Related);

        AssertError(DomainErrors.Relations.DuplicateRelation, reversed);
        Assert.Equal(1, await world.Db.DocumentRelations.CountAsync());

        var fromSecond = await world.ListAsync(second);
        Assert.Equal(RelationDirection.Symmetric, Assert.Single(fromSecond.Value.Items).Direction);
    }

    [Fact]
    public async Task Create_SameLinkTwice_IsRejectedAsDuplicate()
    {
        var world = await CreateWorldAsync();
        var from = await world.DocumentAsync();
        var to = await world.LinkedAsync(from);

        AssertError(DomainErrors.Relations.DuplicateRelation, await world.LinkAsync(from, to, ReplyTo));
    }

    [Fact]
    public async Task Create_DirectAndIndirectLoops_AreRejectedPerType()
    {
        var world = await CreateWorldAsync();
        var a = await world.DocumentAsync("أ");
        var b = await world.LinkedAsync(a, ReplyTo, "ب");
        var c = await world.LinkedAsync(b, ReplyTo, "ج");

        AssertError(DomainErrors.Relations.CycleDetected, await world.LinkAsync(b, a, ReplyTo));
        Assert.True((await world.LinkAsync(a, b, DocumentRelationType.JoinedIntoId)).IsSuccess);
        AssertError(DomainErrors.Relations.CycleDetected, await world.LinkAsync(b, a, DocumentRelationType.JoinedIntoId));
        AssertError(DomainErrors.Relations.CycleDetected, await world.LinkAsync(c, a, ReplyTo));

        // A loop is only a loop within one type, and a symmetric type never forms one.
        Assert.True((await world.LinkAsync(c, a, Supersedes)).IsSuccess);
        Assert.True((await world.LinkAsync(c, a, Related)).IsSuccess);
        Assert.True((await world.LinkAsync(a, c, ReplyTo)).IsSuccess);
    }

    [Fact]
    public async Task Create_ToItself_OrWithUnknownOrInactiveType_IsRejected()
    {
        var world = await CreateWorldAsync();
        var from = await world.DocumentAsync();
        var to = await world.DocumentAsync("آخر");

        AssertError(DomainErrors.Relations.SelfRelation, await world.LinkAsync(from, from, ReplyTo));
        AssertError(DomainErrors.Relations.RelationTypeNotFound, await world.LinkAsync(from, to, 404));
        AssertError(DomainErrors.Relations.RelationTypeInactive, await world.LinkAsync(from, to, Retired));
        AssertError(DomainErrors.Relations.NotesTooLong, await world.LinkAsync(from, to, ReplyTo, new string('ن', 501)));
        Assert.Empty(world.Db.DocumentRelations);
    }

    [Fact]
    public async Task Create_RejectsAnArchivedAnchor_ButAllowsAnArchivedTarget()
    {
        var world = await CreateWorldAsync();
        var archived = await world.DocumentAsync("مؤرشف");
        Assert.True(archived.Register(Guid.NewGuid(), OfficialNumberValue.Create("N-1").Value, world.Author.UserId).IsSuccess);
        Assert.True(archived.Archive(world.Author.UserId, false, ArchiveLocation.PhysicalArchiveId).IsSuccess);
        await world.Db.SaveChangesAsync();
        var live = await world.DocumentAsync("حي");

        AssertError(DomainErrors.Correspondence.DocumentArchived, await world.LinkAsync(archived, live, ReplyTo));
        Assert.True((await world.LinkAsync(live, archived, ReplyTo)).IsSuccess);
    }

    [Fact]
    public async Task Create_RejectsACancelledDocumentOnEitherSide()
    {
        var world = await CreateWorldAsync();
        var cancelled = await world.DocumentAsync("ملغى");
        Assert.True(cancelled.Cancel(world.Author.UserId, false, "خطأ").IsSuccess);
        await world.Db.SaveChangesAsync();
        var live = await world.DocumentAsync("حي");

        AssertError(DomainErrors.Relations.DocumentCancelled, await world.LinkAsync(live, cancelled, ReplyTo));
        AssertError(DomainErrors.Relations.DocumentCancelled, await world.LinkAsync(cancelled, live, ReplyTo));
    }

    [Fact]
    public async Task Create_NeedsDownloadOnTheAnchorAndViewOnTheTarget()
    {
        var world = await CreateWorldAsync();
        var anchor = await world.DocumentAsync("المسار");
        var target = await world.DocumentAsync("الهدف");

        world.Access.AllowAll = false;
        AssertError(DomainErrors.Correspondence.DocumentAccessDenied, await world.LinkAsync(anchor, target, ReplyTo));

        world.Access.DownloadableDocuments.Add(anchor.DocumentId);
        AssertError(DomainErrors.Correspondence.DocumentAccessDenied, await world.LinkAsync(anchor, target, ReplyTo));

        world.Access.ViewableDocuments.Add(target.DocumentId);
        Assert.True((await world.LinkAsync(anchor, target, ReplyTo)).IsSuccess);
    }

    [Fact]
    public async Task Create_AnUnknownDocument_IsNotFound()
    {
        var world = await CreateWorldAsync();
        var anchor = await world.DocumentAsync();
        var ghost = Document.CreateDraft("شبح", DocumentType.InternalMemoId, DocumentDirection.InternalId, world.Home.DepartmentId, world.Author.UserId).Value;

        AssertError(DomainErrors.Correspondence.DocumentNotFound, await world.LinkAsync(anchor, ghost, ReplyTo));
        AssertError(DomainErrors.Correspondence.DocumentNotFound, await world.LinkAsync(ghost, anchor, ReplyTo));
    }

    // ---- Remove --------------------------------------------------------------------------------

    [Fact]
    public async Task Remove_DeletesTheRowAndKeepsTheHistory()
    {
        var world = await CreateWorldAsync();
        var from = await world.DocumentAsync();
        var to = await world.DocumentAsync("آخر");
        var relationId = (await world.LinkAsync(from, to, ReplyTo)).Value.RelationId;

        // Either end of the link may remove it.
        var result = await world.UnlinkAsync(to, relationId, " ربط خاطئ ");

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Empty(world.Db.DocumentRelations);
        var events = await world.Db.DocumentRelationEvents.OrderBy(e => e.OccurredAt).ToListAsync();
        Assert.Equal(new[] { RelationActionType.CreatedId, RelationActionType.RemovedId }, events.Select(e => e.ActionTypeId).Order());
        Assert.Equal("ربط خاطئ", events.Single(e => e.ActionTypeId == RelationActionType.RemovedId).Reason);

        // The same link can be made again once it is gone.
        Assert.True((await world.LinkAsync(from, to, ReplyTo)).IsSuccess);
    }

    [Fact]
    public async Task Remove_NeedsAReason_AnOwnLink_AndAnUnarchivedDocument()
    {
        var world = await CreateWorldAsync();
        var from = await world.DocumentAsync();
        var to = await world.DocumentAsync("آخر");
        var stray = await world.DocumentAsync("غريب");
        var relationId = (await world.LinkAsync(from, to, ReplyTo)).Value.RelationId;

        AssertError(DomainErrors.Relations.ReasonRequired, await world.UnlinkAsync(from, relationId, "  "));
        AssertError(DomainErrors.Relations.ReasonTooLong, await world.UnlinkAsync(from, relationId, new string('س', 501)));
        AssertError(DomainErrors.Relations.RelationNotFound, await world.UnlinkAsync(stray, relationId));
        AssertError(DomainErrors.Relations.RelationNotFound, await world.UnlinkAsync(from, Guid.NewGuid()));
        Assert.Equal(1, await world.Db.DocumentRelations.CountAsync());

        Assert.True(from.Register(Guid.NewGuid(), OfficialNumberValue.Create("N-2").Value, world.Author.UserId).IsSuccess);
        Assert.True(from.Archive(world.Author.UserId, false, ArchiveLocation.PhysicalArchiveId).IsSuccess);
        await world.Db.SaveChangesAsync();
        AssertError(DomainErrors.Correspondence.DocumentArchived, await world.UnlinkAsync(from, relationId));
    }

    // ---- Reads ---------------------------------------------------------------------------------

    [Fact]
    public async Task List_ShowsBothDirections_AndCountsLinksToDocumentsTheCallerCannotView()
    {
        var world = await CreateWorldAsync();
        var middle = await world.DocumentAsync("وسط");
        var earlier = await world.DocumentAsync("سابق");
        var later = await world.DocumentAsync("لاحق");
        var secret = await world.DocumentAsync("سري", world.Stranger);
        Assert.True((await world.LinkAsync(middle, earlier, ReplyTo)).IsSuccess);
        Assert.True((await world.LinkAsync(later, middle, ReplyTo)).IsSuccess);
        Assert.True((await world.LinkAsync(middle, secret, Related)).IsSuccess);

        var result = await world.ListAsync(middle);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(1, result.Value.HiddenRelationCount);
        var byTitle = result.Value.Items.ToDictionary(item => item.RelatedDocument.Title);
        Assert.Equal(2, byTitle.Count);
        Assert.Equal(RelationDirection.Outgoing, byTitle["سابق"].Direction);
        Assert.Equal(RelationDirection.Incoming, byTitle["لاحق"].Direction);
        Assert.True(byTitle["سابق"].Type.PreventCycles);
        Assert.DoesNotContain(result.Value.Items, item => item.RelatedDocument.DocumentId == secret.DocumentId);
    }

    [Fact]
    public async Task List_NeedsViewAccess()
    {
        var world = await CreateWorldAsync();
        var document = await world.DocumentAsync();
        world.Access.AllowAll = false;

        AssertError(DomainErrors.Correspondence.DocumentAccessDenied, await world.ListAsync(document));
    }

    [Fact]
    public async Task Graph_FollowsLinksToTheRequestedDepth()
    {
        var world = await CreateWorldAsync();
        var root = await world.DocumentAsync("جذر");
        var one = await world.LinkedAsync(root, ReplyTo, "واحد");
        var two = await world.LinkedAsync(one, ReplyTo, "اثنان");
        await world.LinkedAsync(two, ReplyTo, "ثلاثة");

        var shallow = await world.GraphAsync(root, depth: 1);
        var deeper = await world.GraphAsync(root, depth: 2);

        Assert.Equal(new[] { "جذر", "واحد" }, shallow.Value.Nodes.Select(n => n.Document.Title));
        Assert.Single(shallow.Value.Edges);
        Assert.Equal(3, deeper.Value.Nodes.Count);
        Assert.Equal(2, deeper.Value.Edges.Count);
        Assert.Equal(new[] { 0, 1, 2 }, deeper.Value.Nodes.Select(n => n.Depth));
        Assert.Equal(ReplyTo, Assert.Single(deeper.Value.Types).RelationTypeId);
        Assert.False(deeper.Value.Truncated);
    }

    [Fact]
    public async Task Graph_NeverEntersOrTraversesADocumentTheCallerCannotView()
    {
        var world = await CreateWorldAsync();
        var root = await world.DocumentAsync("جذر");
        var hidden = await world.DocumentAsync("مخفي", world.Stranger);
        var behind = await world.DocumentAsync("خلف المخفي");
        Assert.True((await world.LinkAsync(root, hidden, ReplyTo)).IsSuccess);
        Assert.True((await world.LinkAsync(hidden, behind, ReplyTo)).IsSuccess);

        var result = await world.GraphAsync(root, depth: 3);

        Assert.Equal(new[] { root.DocumentId }, result.Value.Nodes.Select(n => n.Document.DocumentId));
        Assert.Empty(result.Value.Edges);
        Assert.Equal(1, result.Value.HiddenRelationCount);
    }

    [Fact]
    public async Task Graph_RejectsAnOutOfRangeDepth_AndAnUnreadableRoot()
    {
        var world = await CreateWorldAsync();
        var root = await world.DocumentAsync();

        AssertError(DomainErrors.Relations.GraphDepthInvalid, await world.GraphAsync(root, depth: 0));
        AssertError(DomainErrors.Relations.GraphDepthInvalid, await world.GraphAsync(root, depth: 4));

        world.Access.AllowAll = false;
        AssertError(DomainErrors.Correspondence.DocumentAccessDenied, await world.GraphAsync(root));
    }

    [Fact]
    public async Task Events_ListsCreatedAndRemovedLinksNewestFirst()
    {
        var world = await CreateWorldAsync();
        var from = await world.DocumentAsync();
        var to = await world.DocumentAsync("آخر");
        var relationId = (await world.LinkAsync(from, to, ReplyTo)).Value.RelationId;
        Assert.True((await world.UnlinkAsync(from, relationId)).IsSuccess);

        var result = await new ListDocumentRelationEventsQueryHandler(world.Documents, world.Relations, world.Access)
            .Handle(new ListDocumentRelationEventsQuery(to.DocumentId), default);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(2, result.Value.TotalCount);
        Assert.Equal(
            new[] { RelationActionType.RemovedId, RelationActionType.CreatedId },
            result.Value.Items.Select(e => e.ActionTypeId));
    }

    // ---- Constraints and audit -----------------------------------------------------------------

    [Fact]
    public async Task Constraints_RejectUnknownDocuments_AndAnIdenticalSecondRow()
    {
        var world = await CreateWorldAsync();
        var a = await world.DocumentAsync("أ");
        var type = await world.Relations.GetTypeByIdAsync(ReplyTo);

        world.Db.DocumentRelations.Add(DocumentRelation.Create(a.DocumentId, Guid.NewGuid(), type!, world.Author.UserId).Value);
        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
        world.Db.ChangeTracker.Clear();

        var b = await world.DocumentAsync("ب");
        world.Db.DocumentRelations.Add(DocumentRelation.Create(a.DocumentId, b.DocumentId, type!, world.Author.UserId).Value);
        await world.Db.SaveChangesAsync();
        world.Db.DocumentRelations.Add(DocumentRelation.Create(a.DocumentId, b.DocumentId, type!, world.Author.UserId).Value);
        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Constraints_RejectARowThatLinksADocumentToItself()
    {
        var world = await CreateWorldAsync();
        var a = await world.DocumentAsync("أ");

        await Assert.ThrowsAsync<Microsoft.Data.Sqlite.SqliteException>(() => world.Db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO DOCUMENT_RELATIONS (RelationId, SourceDocumentId, TargetDocumentId, RelationTypeId, CreatedByUserId, CreatedAt)
            VALUES ({Guid.NewGuid()}, {a.DocumentId}, {a.DocumentId}, {ReplyTo}, {world.Author.UserId}, {Now()})
            """));
    }

    [Fact]
    public async Task Audit_WritesOneRowForCreateAndOneForRemoveCarryingTheLinkedDocuments()
    {
        var accessor = new HttpContextAccessor();
        var db = TestContextFactory.Create(new TamperEvidentAuditInterceptor(accessor, TestClock.Instance));
        TestContextFactory.SeedAuditLookups(db);
        var world = await CreateWorldAsync(db);
        var from = await world.DocumentAsync();
        var to = await world.DocumentAsync("آخر");

        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, world.Author.UserId.ToString()),
                new Claim("dept_id", world.Author.PrimaryDepartmentId.ToString())
            }, authenticationType: "Test"))
        };

        var relationId = (await world.LinkAsync(from, to, ReplyTo)).Value.RelationId;
        var created = await db.AuditLogs.SingleAsync(log => log.ModuleName == "RELATIONS");
        Assert.Equal(AuditActionType.CreateId, created.ActionTypeId);
        Assert.Equal(relationId.ToString(), created.EntityId);
        Assert.Equal(from.DocumentId, created.RelatedDocumentId);
        Assert.Contains(to.DocumentId.ToString(), created.NewValuesJson);

        Assert.True((await world.UnlinkAsync(from, relationId)).IsSuccess);

        var rows = await db.AuditLogs.Where(log => log.ModuleName == "RELATIONS").OrderBy(log => log.AuditLogId).ToListAsync();
        Assert.Equal(2, rows.Count);
        Assert.Equal(AuditActionType.DeleteId, rows[1].ActionTypeId);
        Assert.Contains(to.DocumentId.ToString(), rows[1].OldValuesJson);
    }

    // ---- Domain --------------------------------------------------------------------------------

    [Fact]
    public void Domain_RemoveNeedsAReasonAndRaisesTheRemovedEvent()
    {
        var type = DocumentRelationType.Create(ReplyTo, "Reply to", "ردّ على").Value;
        var relation = DocumentRelation.Create(Guid.NewGuid(), Guid.NewGuid(), type, Guid.NewGuid()).Value;
        relation.ClearDomainEvents();

        AssertError(DomainErrors.Relations.ReasonRequired, relation.Remove(Guid.NewGuid(), " "));
        AssertError(DomainErrors.Relations.ReasonTooLong, relation.Remove(Guid.NewGuid(), new string('x', 501)));
        Assert.Empty(relation.DomainEvents);

        Assert.True(relation.Remove(Guid.NewGuid(), "سبب").IsSuccess);
        Assert.IsType<InstitutionalDiwan.Domain.Events.DocumentRelationRemovedEvent>(Assert.Single(relation.DomainEvents));
    }

    [Fact]
    public void Domain_SymmetricEndpointsAreOrderedSoTheReverseLinkIsTheSameRow()
    {
        var type = DocumentRelationType.Create(Related, "Related", "مرتبط", isSymmetric: true, preventCycles: false).Value;
        var low = new Guid("00000000-0000-0000-0000-000000000001");
        var high = new Guid("00000000-0000-0000-0000-000000000002");

        var forward = DocumentRelation.Create(low, high, type, Guid.NewGuid()).Value;
        var backward = DocumentRelation.Create(high, low, type, Guid.NewGuid()).Value;

        Assert.Equal((forward.SourceDocumentId, forward.TargetDocumentId), (backward.SourceDocumentId, backward.TargetDocumentId));
    }

    private static DateTime Now() => TestClock.Instance.UtcNow;
}
