using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Infrastructure.Authorization;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Holds the <c>VIEW</c> rule in the document list SQL equal to <see cref="DocumentAccessEvaluator"/>
/// against a real SQLite database: for every kind of caller, a list returns exactly the documents the
/// evaluator lets them view, and its count agrees.
/// </summary>
public sealed class DocumentViewAccessQueryTests
{
    private static readonly DateTime Now = TestClock.Instance.UtcNow;

    private sealed record World(
        DiwanDbContext Db,
        Department Parent,
        Department Child,
        Department Other,
        Department Remote,
        User Author);

    private static void SeedLookups(DiwanDbContext db)
    {
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
        foreach (var id in new[] { DocumentStatusType.DraftId, DocumentStatusType.RegisteredId, DocumentStatusType.ArchivedId })
            db.DocumentStatusTypes.Add(DocumentStatusType.Create(id, $"Status {id}", $"حالة {id}").Value);

        db.ArchiveLocations.Add(ArchiveLocation.Create(ArchiveLocation.PhysicalArchiveId, "Physical", "ورقي").Value);

        db.MovementTypes.Add(MovementType.Create(MovementType.ReferralId, "Referral", "إحالة").Value);
        foreach (var id in new[] { MovementStatus.DraftId, MovementStatus.SentId, MovementStatus.ClosedId, MovementStatus.CanceledId })
            db.MovementStatuses.Add(MovementStatus.Create(id, $"Status {id}", $"حالة {id}").Value);
        foreach (var id in new[] { MovementStepStatus.PendingId, MovementStepStatus.SentId, MovementStepStatus.RedirectedId })
            db.MovementStepStatuses.Add(MovementStepStatus.Create(id, $"Step {id}", $"خطوة {id}").Value);
        foreach (var id in new[] { MovementEventType.CreatedId, MovementEventType.SentId, MovementEventType.ClosedId, MovementEventType.RedirectedId })
            db.MovementEventTypes.Add(MovementEventType.Create(id, $"Event {id}", $"حدث {id}").Value);
    }

    private static async Task<World> CreateWorldAsync()
    {
        var db = TestContextFactory.Create();
        SeedLookups(db);

        var parent = Department.Create("PARENT", "الأصل", DepartmentType.SectionId).Value;
        var child = Department.Create("CHILD", "الفرع", DepartmentType.SectionId, parent.DepartmentId).Value;
        var other = Department.Create("OTHER", "أخرى", DepartmentType.SectionId).Value;
        var remote = Department.Create("REMOTE", "بعيدة", DepartmentType.SectionId).Value;
        db.Departments.AddRange(parent, child, other, remote);

        var author = User.Create("author", "hash", remote.DepartmentId).Value;
        db.Users.Add(author);

        await db.SaveChangesAsync();
        return new World(db, parent, child, other, remote, author);
    }

    private static Document NewDocument(
        World world,
        Guid departmentId,
        int confidentiality = DocumentConfidentialityLevel.NormalId,
        Guid? createdBy = null)
    {
        var document = Document.CreateDraft(
            "كتاب",
            DocumentType.InternalMemoId,
            DocumentDirection.InternalId,
            departmentId,
            createdBy ?? world.Author.UserId,
            confidentialityLevelId: confidentiality).Value;
        world.Db.Documents.Add(document);
        return document;
    }

    private static void Register(World world, Document document) =>
        Assert.True(document.Register(
            Guid.NewGuid(),
            OfficialNumberValue.Create($"N-{Guid.NewGuid():N}"[..12]).Value,
            world.Author.UserId).IsSuccess);

    private static (DepartmentScopeEvaluator Scope, DocumentAccessEvaluator Evaluator) Evaluators(World world, User caller)
    {
        var userContext = new FakeUserContext
        {
            IsAuthenticated = true,
            UserId = caller.UserId,
            PrimaryDepartmentId = caller.PrimaryDepartmentId,
            ScopeTypeId = caller.ScopeTypeId
        };
        var db = world.Db;
        var scope = new DepartmentScopeEvaluator(userContext, new UserRepository(db), new DepartmentRepository(db), TestClock.Instance);
        var evaluator = new DocumentAccessEvaluator(
            userContext, new DocumentRepository(db), new DocumentAccessGrantRepository(db), scope, new DocumentMovementRepository(db), TestClock.Instance);
        return (scope, evaluator);
    }

    private static async Task<DocumentViewAccess> AccessAsync(DepartmentScopeEvaluator scope, User caller) =>
        new(caller.UserId, await scope.GetAccessibleDepartmentIdsAsync(), Now);

    private static PageRequest AllRows() => new(1, 100, null, null, true);

    [Theory]
    [InlineData(UserScopeType.DepartmentId, false)]
    [InlineData(UserScopeType.DepartmentId, true)]
    [InlineData(UserScopeType.DepartmentTreeId, false)]
    [InlineData(UserScopeType.InstitutionId, false)]
    [InlineData(UserScopeType.SystemId, false)]
    public async Task List_MatchesDocumentAccessEvaluator(int scopeTypeId, bool withSubtreeAssignment)
    {
        var world = await CreateWorldAsync();
        var db = world.Db;

        var caller = User.Create("caller", "hash", world.Parent.DepartmentId, scopeTypeId: scopeTypeId).Value;
        if (withSubtreeAssignment)
            Assert.True(caller.AssignDepartmentScope(world.Other.DepartmentId, includeSubtree: true, startDate: Now.AddDays(-1)).IsSuccess);
        db.Users.Add(caller);

        var documents = new List<Document>();
        foreach (var department in new[] { world.Parent, world.Child, world.Other, world.Remote })
        {
            foreach (var confidentiality in new[] { DocumentConfidentialityLevel.NormalId, DocumentConfidentialityLevel.SecretId, DocumentConfidentialityLevel.TopSecretId })
                documents.Add(NewDocument(world, department.DepartmentId, confidentiality));
        }

        var authored = NewDocument(world, world.Remote.DepartmentId, DocumentConfidentialityLevel.TopSecretId, caller.UserId);
        var granted = NewDocument(world, world.Remote.DepartmentId, DocumentConfidentialityLevel.SecretId);
        var grantedDownload = NewDocument(world, world.Remote.DepartmentId);
        var expired = NewDocument(world, world.Remote.DepartmentId, DocumentConfidentialityLevel.SecretId);
        var revoked = NewDocument(world, world.Remote.DepartmentId, DocumentConfidentialityLevel.SecretId);
        var grantToSomeoneElse = NewDocument(world, world.Remote.DepartmentId, DocumentConfidentialityLevel.SecretId);
        documents.AddRange(new[] { authored, granted, grantedDownload, expired, revoked, grantToSomeoneElse });

        db.DocumentAccessGrants.Add(DocumentAccessGrant.Grant(granted.DocumentId, caller.UserId, AccessLevel.ViewId, world.Author.UserId, utcNow: Now).Value);
        db.DocumentAccessGrants.Add(DocumentAccessGrant.Grant(grantedDownload.DocumentId, caller.UserId, AccessLevel.DownloadId, world.Author.UserId, expiresAt: Now.AddDays(1), utcNow: Now).Value);
        db.DocumentAccessGrants.Add(DocumentAccessGrant.Grant(expired.DocumentId, caller.UserId, AccessLevel.ManageId, world.Author.UserId, expiresAt: Now.AddDays(-1), utcNow: Now.AddDays(-2)).Value);
        var revokedGrant = DocumentAccessGrant.Grant(revoked.DocumentId, caller.UserId, AccessLevel.ManageId, world.Author.UserId, utcNow: Now).Value;
        Assert.True(revokedGrant.Revoke(world.Author.UserId, Now).IsSuccess);
        db.DocumentAccessGrants.Add(revokedGrant);
        db.DocumentAccessGrants.Add(DocumentAccessGrant.Grant(grantToSomeoneElse.DocumentId, world.Author.UserId, AccessLevel.ManageId, world.Author.UserId, utcNow: Now).Value);

        // The routed documents are owned outside the caller's scope, so a movement is their only way in.
        Document Routed(int confidentiality = DocumentConfidentialityLevel.NormalId)
        {
            var document = NewDocument(world, world.Remote.DepartmentId, confidentiality);
            Register(world, document);
            documents.Add(document);
            return document;
        }

        DocumentMovement Movement(Document document, Guid from, Guid to)
        {
            var movement = DocumentMovement.Create(document.DocumentId, from, to, world.Author.UserId).Value;
            db.DocumentMovements.Add(movement);
            return movement;
        }

        var inFlight = Routed();
        Assert.True(Movement(inFlight, world.Remote.DepartmentId, world.Parent.DepartmentId).Send(world.Author.UserId).IsSuccess);

        var sentByCaller = Routed();
        Assert.True(Movement(sentByCaller, world.Parent.DepartmentId, world.Remote.DepartmentId).Send(world.Author.UserId).IsSuccess);

        var secretInFlight = Routed(DocumentConfidentialityLevel.SecretId);
        Assert.True(Movement(secretInFlight, world.Remote.DepartmentId, world.Parent.DepartmentId).Send(world.Author.UserId).IsSuccess);

        var notDispatched = Routed();
        Movement(notDispatched, world.Remote.DepartmentId, world.Parent.DepartmentId);

        var closed = Routed();
        var closedMovement = Movement(closed, world.Remote.DepartmentId, world.Parent.DepartmentId);
        Assert.True(closedMovement.Send(world.Author.UserId).IsSuccess);
        Assert.True(closedMovement.Close(world.Author.UserId).IsSuccess);

        var redirectedAway = Routed();
        var far = Department.Create("FAR", "نائية", DepartmentType.SectionId).Value;
        db.Departments.Add(far);
        var redirectedMovement = Movement(redirectedAway, world.Parent.DepartmentId, world.Remote.DepartmentId);
        Assert.True(redirectedMovement.Send(world.Author.UserId).IsSuccess);
        Assert.True(redirectedMovement.RedirectTo(far.DepartmentId, world.Author.UserId).IsSuccess);

        var archived = NewDocument(world, world.Parent.DepartmentId);
        Register(world, archived);
        Assert.True(archived.Archive(world.Author.UserId, hasActiveMovements: false, ArchiveLocation.PhysicalArchiveId).IsSuccess);
        var archivedSecret = NewDocument(world, world.Parent.DepartmentId, DocumentConfidentialityLevel.SecretId);
        Register(world, archivedSecret);
        Assert.True(archivedSecret.Archive(world.Author.UserId, hasActiveMovements: false, ArchiveLocation.PhysicalArchiveId).IsSuccess);
        documents.AddRange(new[] { archived, archivedSecret });

        await db.SaveChangesAsync();

        var (scope, evaluator) = Evaluators(world, caller);

        var expected = new HashSet<Guid>();
        foreach (var document in documents)
        {
            if (await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ViewId))
                expected.Add(document.DocumentId);
        }

        var result = await new DocumentRepository(db).ListPagedAsync(AllRows(), await AccessAsync(scope, caller));

        Assert.Equal(expected, result.Items.Select(document => document.DocumentId).ToHashSet());
        Assert.Equal(expected.Count, result.TotalCount);
        Assert.Equal(
            expected,
            (await new DocumentRepository(db).ListViewableIdsAsync(
                documents.Select(document => document.DocumentId).ToList(),
                await AccessAsync(scope, caller))).ToHashSet());

        // The evaluator is the reference above; these pin the rule itself for every caller.
        Assert.Contains(authored.DocumentId, expected);
        Assert.Contains(granted.DocumentId, expected);
        Assert.Contains(grantedDownload.DocumentId, expected);
        Assert.Contains(inFlight.DocumentId, expected);
        Assert.Contains(sentByCaller.DocumentId, expected);
        Assert.Contains(archived.DocumentId, expected);
        Assert.DoesNotContain(expired.DocumentId, expected);
        Assert.DoesNotContain(revoked.DocumentId, expected);
        Assert.DoesNotContain(grantToSomeoneElse.DocumentId, expected);
        Assert.DoesNotContain(secretInFlight.DocumentId, expected);
        Assert.DoesNotContain(archivedSecret.DocumentId, expected);

        var institutionWide = scopeTypeId is UserScopeType.InstitutionId or UserScopeType.SystemId;
        Assert.Equal(institutionWide, expected.Contains(notDispatched.DocumentId));
        Assert.Equal(institutionWide, expected.Contains(closed.DocumentId));
        Assert.Equal(institutionWide, expected.Contains(redirectedAway.DocumentId));
    }

    [Fact]
    public async Task List_ArchivedStatusFilter_DoesNotReachBeyondTheCallersAccess()
    {
        var world = await CreateWorldAsync();
        var db = world.Db;
        var caller = User.Create("caller", "hash", world.Parent.DepartmentId).Value;
        db.Users.Add(caller);

        var visible = NewDocument(world, world.Parent.DepartmentId);
        var secret = NewDocument(world, world.Parent.DepartmentId, DocumentConfidentialityLevel.SecretId);
        var outOfScope = NewDocument(world, world.Remote.DepartmentId);
        foreach (var document in new[] { visible, secret, outOfScope })
        {
            Register(world, document);
            Assert.True(document.Archive(world.Author.UserId, hasActiveMovements: false, ArchiveLocation.PhysicalArchiveId).IsSuccess);
        }
        await db.SaveChangesAsync();

        var (scope, _) = Evaluators(world, caller);
        var result = await new DocumentRepository(db).ListPagedAsync(
            AllRows(), await AccessAsync(scope, caller), statusId: DocumentStatusType.ArchivedId);

        Assert.Equal(visible.DocumentId, Assert.Single(result.Items).DocumentId);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task List_PagesOverViewableDocumentsOnly()
    {
        var world = await CreateWorldAsync();
        var db = world.Db;
        var caller = User.Create("caller", "hash", world.Parent.DepartmentId).Value;
        db.Users.Add(caller);

        for (var i = 0; i < 5; i++)
        {
            NewDocument(world, world.Parent.DepartmentId);
            NewDocument(world, world.Parent.DepartmentId, DocumentConfidentialityLevel.SecretId);
            NewDocument(world, world.Remote.DepartmentId);
        }
        await db.SaveChangesAsync();

        var (scope, _) = Evaluators(world, caller);
        var result = await new DocumentRepository(db).ListPagedAsync(
            new PageRequest(2, 2, null, null, true), await AccessAsync(scope, caller));

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.All(result.Items, document =>
        {
            Assert.Equal(world.Parent.DepartmentId, document.OwningDepartmentId);
            Assert.Equal(DocumentConfidentialityLevel.NormalId, document.ConfidentialityLevelId);
        });
    }
}
