using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using InstitutionalDiwan.Infrastructure.Authorization;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Interceptors;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Exercises the archive tables against a real SQLite database: search access and paging, the
/// foreign keys and unique indexes, and the audit rows written for an archive transition.
/// </summary>
public sealed class ArchivingRepositoryTests
{
    private static readonly DateTime Now = TestClock.Instance.UtcNow;

    private sealed record World(
        DiwanDbContext Db,
        Department Parent,
        Department Child,
        Department Other,
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

        db.ArchiveEventTypes.Add(ArchiveEventType.Create(ArchiveEventType.ArchivedId, "Archived", "أرشفة").Value);
        db.ArchiveEventTypes.Add(ArchiveEventType.Create(ArchiveEventType.UnarchivedId, "Unarchived", "إلغاء الأرشفة").Value);
        db.ArchiveLocations.Add(ArchiveLocation.Create(ArchiveLocation.PhysicalArchiveId, "Physical", "ورقي").Value);
        db.ArchiveLocations.Add(ArchiveLocation.Create(ArchiveLocation.ElectronicArchiveId, "Electronic", "إلكتروني").Value);
    }

    private static async Task<World> CreateWorldAsync(DiwanDbContext? existing = null)
    {
        var db = existing ?? TestContextFactory.Create();
        SeedLookups(db);

        var parent = Department.Create("PARENT", "الأصل", DepartmentType.SectionId).Value;
        var child = Department.Create("CHILD", "الفرع", DepartmentType.SectionId, parent.DepartmentId).Value;
        var other = Department.Create("OTHER", "أخرى", DepartmentType.SectionId).Value;
        db.Departments.AddRange(parent, child, other);

        var author = User.Create("author", "hash", other.DepartmentId).Value;
        db.Users.Add(author);

        await db.SaveChangesAsync();
        return new World(db, parent, child, other, author);
    }

    private static async Task<Document> ArchivedDocumentAsync(
        World world,
        Guid departmentId,
        string title = "كتاب",
        int confidentiality = DocumentConfidentialityLevel.NormalId,
        int location = ArchiveLocation.PhysicalArchiveId,
        Guid? createdBy = null,
        DateTime? archivedAt = null)
    {
        var at = archivedAt ?? Now;
        var document = Document.CreateDraft(
            title, DocumentType.InternalMemoId, DocumentDirection.InternalId, departmentId,
            createdBy ?? world.Author.UserId, confidentialityLevelId: confidentiality).Value;
        Assert.True(document.Register(Guid.NewGuid(), OfficialNumberValue.Create($"N-{Guid.NewGuid():N}"[..12]).Value, world.Author.UserId).IsSuccess);
        Assert.True(document.Archive(world.Author.UserId, hasActiveMovements: false, location, utcNow: at).IsSuccess);
        world.Db.Documents.Add(document);
        world.Db.DocumentArchiveEvents.Add(ArchiveEvent(world, document.DocumentId, ArchiveEventType.ArchivedId, location, at));
        await world.Db.SaveChangesAsync();
        return document;
    }

    private static DocumentArchiveEvent ArchiveEvent(
        World world,
        Guid documentId,
        int eventTypeId,
        int? location,
        DateTime at) =>
        DocumentArchiveEvent.Record(
            documentId,
            eventTypeId,
            DocumentStatusType.RegisteredId,
            DocumentStatusType.ArchivedId,
            world.Author.UserId,
            archiveLocationId: location,
            utcNow: at).Value;

    private static ArchiveSearchFilter EveryDepartment(
        Guid? caller = null,
        Guid? owningDepartmentId = null,
        int? archiveLocationId = null,
        DateTime? from = null,
        DateTime? to = null) =>
        new(caller ?? Guid.NewGuid(), null, Now, owningDepartmentId, ArchiveLocationId: archiveLocationId, ArchivedFromUtc: from, ArchivedToUtc: to);

    private static PageRequest Page(string? search = null, string? sortBy = null, bool descending = true, int page = 1, int size = 20) =>
        new(page, size, search, sortBy, descending);

    [Fact]
    public async Task Search_ReturnsOnlyArchivedDocumentsWithTheirLatestArchiveEvent()
    {
        var world = await CreateWorldAsync();
        var archived = await ArchivedDocumentAsync(world, world.Parent.DepartmentId, archivedAt: Now.AddDays(-3));

        // Archived, restored and archived again: one row, carrying the second event.
        world.Db.DocumentArchiveEvents.Add(ArchiveEvent(world, archived.DocumentId, ArchiveEventType.UnarchivedId, null, Now.AddDays(-2)));
        world.Db.DocumentArchiveEvents.Add(ArchiveEvent(world, archived.DocumentId, ArchiveEventType.ArchivedId, ArchiveLocation.ElectronicArchiveId, Now.AddDays(-1)));

        var restored = await ArchivedDocumentAsync(world, world.Parent.DepartmentId);
        Assert.True(restored.Unarchive(world.Author.UserId, "سبب").IsSuccess);
        await world.Db.SaveChangesAsync();

        var result = await new DocumentArchiveRepository(world.Db).SearchAsync(EveryDepartment(), Page());

        var row = Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(archived.DocumentId, row.Document.DocumentId);
        Assert.Equal(Now.AddDays(-1), row.ArchivedAt);
        Assert.Equal(ArchiveLocation.ElectronicArchiveId, row.ArchiveLocationId);
        Assert.Equal(world.Author.UserId, row.ArchivedByUserId);
    }

    [Fact]
    public async Task Search_AppliesFiltersSearchSortAndPaging()
    {
        var world = await CreateWorldAsync();
        var oldest = await ArchivedDocumentAsync(world, world.Parent.DepartmentId, "ألف", archivedAt: Now.AddDays(-3));
        var middle = await ArchivedDocumentAsync(world, world.Parent.DepartmentId, "باء", location: ArchiveLocation.ElectronicArchiveId, archivedAt: Now.AddDays(-2));
        var newest = await ArchivedDocumentAsync(world, world.Child.DepartmentId, "تاء", archivedAt: Now.AddDays(-1));
        var repository = new DocumentArchiveRepository(world.Db);

        async Task<Guid[]> IdsAsync(ArchiveSearchFilter filter, PageRequest page) =>
            (await repository.SearchAsync(filter, page)).Items.Select(row => row.Document.DocumentId).ToArray();

        Assert.Equal(new[] { newest.DocumentId, middle.DocumentId, oldest.DocumentId }, await IdsAsync(EveryDepartment(), Page()));
        Assert.Equal(new[] { oldest.DocumentId, middle.DocumentId, newest.DocumentId }, await IdsAsync(EveryDepartment(), Page(sortBy: "title", descending: false)));
        Assert.Equal(new[] { newest.DocumentId }, await IdsAsync(EveryDepartment(owningDepartmentId: world.Child.DepartmentId), Page()));
        Assert.Equal(new[] { middle.DocumentId }, await IdsAsync(EveryDepartment(archiveLocationId: ArchiveLocation.ElectronicArchiveId), Page()));
        Assert.Equal(new[] { middle.DocumentId }, await IdsAsync(EveryDepartment(from: Now.AddDays(-2.5), to: Now.AddDays(-1.5)), Page()));
        Assert.Equal(new[] { oldest.DocumentId }, await IdsAsync(EveryDepartment(), Page(search: "ألف")));

        var secondPage = await repository.SearchAsync(EveryDepartment(), Page(page: 2, size: 2));
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(oldest.DocumentId, Assert.Single(secondPage.Items).Document.DocumentId);
    }

    [Fact]
    public async Task Search_SecretDocumentNeverReachesScopeOnlyCaller_NorItsCount()
    {
        var world = await CreateWorldAsync();
        await ArchivedDocumentAsync(world, world.Parent.DepartmentId, confidentiality: DocumentConfidentialityLevel.SecretId);
        var visible = await ArchivedDocumentAsync(world, world.Parent.DepartmentId);
        var filter = new ArchiveSearchFilter(Guid.NewGuid(), new[] { world.Parent.DepartmentId }, Now);

        var result = await new DocumentArchiveRepository(world.Db).SearchAsync(filter, Page());

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(visible.DocumentId, Assert.Single(result.Items).Document.DocumentId);
    }

    /// <summary>
    /// The search repeats the evaluator's VIEW rule in SQL, so the two must agree: for every kind of
    /// caller, the search returns exactly the archived documents the evaluator lets them view.
    /// </summary>
    [Theory]
    [InlineData(UserScopeType.DepartmentId, false)]
    [InlineData(UserScopeType.DepartmentId, true)]
    [InlineData(UserScopeType.DepartmentTreeId, false)]
    [InlineData(UserScopeType.InstitutionId, false)]
    [InlineData(UserScopeType.SystemId, false)]
    public async Task Search_MatchesDocumentAccessEvaluator(int scopeTypeId, bool withSubtreeAssignment)
    {
        var world = await CreateWorldAsync();
        var db = world.Db;

        var caller = User.Create("caller", "hash", world.Parent.DepartmentId, scopeTypeId: scopeTypeId).Value;
        if (withSubtreeAssignment)
            Assert.True(caller.AssignDepartmentScope(world.Other.DepartmentId, includeSubtree: true, startDate: Now.AddDays(-1)).IsSuccess);
        db.Users.Add(caller);
        await db.SaveChangesAsync();

        var documents = new List<Document>();
        foreach (var department in new[] { world.Parent, world.Child, world.Other })
        {
            foreach (var confidentiality in new[] { DocumentConfidentialityLevel.NormalId, DocumentConfidentialityLevel.SecretId, DocumentConfidentialityLevel.TopSecretId })
                documents.Add(await ArchivedDocumentAsync(world, department.DepartmentId, confidentiality: confidentiality));
        }

        var authored = await ArchivedDocumentAsync(world, world.Other.DepartmentId, confidentiality: DocumentConfidentialityLevel.SecretId, createdBy: caller.UserId);
        var granted = await ArchivedDocumentAsync(world, world.Other.DepartmentId, confidentiality: DocumentConfidentialityLevel.TopSecretId);
        var expired = await ArchivedDocumentAsync(world, world.Other.DepartmentId, confidentiality: DocumentConfidentialityLevel.SecretId);
        var revoked = await ArchivedDocumentAsync(world, world.Other.DepartmentId, confidentiality: DocumentConfidentialityLevel.SecretId);
        documents.AddRange(new[] { authored, granted, expired, revoked });

        db.DocumentAccessGrants.Add(DocumentAccessGrant.Grant(granted.DocumentId, caller.UserId, AccessLevel.ViewId, world.Author.UserId, utcNow: Now).Value);
        db.DocumentAccessGrants.Add(DocumentAccessGrant.Grant(expired.DocumentId, caller.UserId, AccessLevel.ManageId, world.Author.UserId, expiresAt: Now.AddDays(-1), utcNow: Now.AddDays(-2)).Value);
        var revokedGrant = DocumentAccessGrant.Grant(revoked.DocumentId, caller.UserId, AccessLevel.ManageId, world.Author.UserId, utcNow: Now).Value;
        Assert.True(revokedGrant.Revoke(world.Author.UserId, Now).IsSuccess);
        db.DocumentAccessGrants.Add(revokedGrant);
        await db.SaveChangesAsync();

        var userContext = new FakeUserContext
        {
            IsAuthenticated = true,
            UserId = caller.UserId,
            PrimaryDepartmentId = caller.PrimaryDepartmentId,
            ScopeTypeId = scopeTypeId
        };
        var scope = new DepartmentScopeEvaluator(userContext, new UserRepository(db), new DepartmentRepository(db), TestClock.Instance);
        var evaluator = new DocumentAccessEvaluator(
            userContext, new DocumentRepository(db), new DocumentAccessGrantRepository(db), scope, new DocumentMovementRepository(db), TestClock.Instance);

        var expected = new HashSet<Guid>();
        foreach (var document in documents)
        {
            if (await evaluator.CanAccessDocumentAsync(document.DocumentId, AccessLevel.ViewId))
                expected.Add(document.DocumentId);
        }

        var result = await new DocumentArchiveRepository(db).SearchAsync(
            new ArchiveSearchFilter(caller.UserId, await scope.GetAccessibleDepartmentIdsAsync(), Now),
            Page(size: 100));

        Assert.Equal(expected, result.Items.Select(row => row.Document.DocumentId).ToHashSet());
        Assert.Equal(expected.Count, result.TotalCount);
        Assert.Contains(authored.DocumentId, expected);
        Assert.Contains(granted.DocumentId, expected);
        Assert.DoesNotContain(expired.DocumentId, expected);
        Assert.DoesNotContain(revoked.DocumentId, expected);
    }

    [Fact]
    public async Task ListByDocument_ReturnsNewestFirstForThatDocumentOnly()
    {
        var world = await CreateWorldAsync();
        var document = await ArchivedDocumentAsync(world, world.Parent.DepartmentId, archivedAt: Now.AddDays(-2));
        await ArchivedDocumentAsync(world, world.Parent.DepartmentId);
        var repository = new DocumentArchiveRepository(world.Db);
        repository.Add(ArchiveEvent(world, document.DocumentId, ArchiveEventType.UnarchivedId, null, Now.AddDays(-1)));
        await world.Db.SaveChangesAsync();

        var events = await repository.ListByDocumentAsync(document.DocumentId);

        Assert.Equal(new[] { ArchiveEventType.UnarchivedId, ArchiveEventType.ArchivedId }, events.Select(e => e.EventTypeId));
    }

    [Fact]
    public async Task ArchiveEvent_RejectsUnknownDocument()
    {
        var world = await CreateWorldAsync();
        world.Db.DocumentArchiveEvents.Add(ArchiveEvent(world, Guid.NewGuid(), ArchiveEventType.ArchivedId, null, Now));

        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task SavedSearches_AreListedByNameAndCountedPerUser()
    {
        var world = await CreateWorldAsync();
        var other = User.Create("other", "hash", world.Other.DepartmentId).Value;
        world.Db.Users.Add(other);
        var repository = new SavedSearchRepository(world.Db);
        repository.Add(SavedSearch.Create(world.Author.UserId, "باء", "{}").Value);
        var first = SavedSearch.Create(world.Author.UserId, "ألف", "{}", isDefault: true).Value;
        repository.Add(first);
        repository.Add(SavedSearch.Create(other.UserId, "ألف", "{}", isDefault: true).Value);
        await world.Db.SaveChangesAsync();

        var names = (await repository.ListByUserAsync(world.Author.UserId)).Select(search => search.Name);

        Assert.Equal(new[] { "ألف", "باء" }, names);
        Assert.Equal(2, await repository.CountByUserAsync(world.Author.UserId));
        Assert.Equal(first.SavedSearchId, (await repository.GetDefaultAsync(world.Author.UserId))!.SavedSearchId);
        Assert.True(await repository.ExistsByNameAsync(world.Author.UserId, " ألف "));
        Assert.False(await repository.ExistsByNameAsync(world.Author.UserId, "ألف", first.SavedSearchId));
        Assert.False(await repository.ExistsByNameAsync(world.Author.UserId, "جيم"));

        repository.Remove(first);
        await world.Db.SaveChangesAsync();

        Assert.Null(await repository.GetDefaultAsync(world.Author.UserId));
    }

    [Fact]
    public async Task SavedSearch_DuplicateNameForSameUser_IsRejectedByIndex()
    {
        var world = await CreateWorldAsync();
        world.Db.SavedSearches.Add(SavedSearch.Create(world.Author.UserId, "بحث", "{}").Value);
        world.Db.SavedSearches.Add(SavedSearch.Create(world.Author.UserId, "بحث", "{}").Value);

        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task SavedSearch_SecondDefaultForSameUser_IsRejectedByIndex()
    {
        var world = await CreateWorldAsync();
        world.Db.SavedSearches.Add(SavedSearch.Create(world.Author.UserId, "أول", "{}", isDefault: true).Value);
        world.Db.SavedSearches.Add(SavedSearch.Create(world.Author.UserId, "ثان", "{}", isDefault: true).Value);

        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task SavedSearch_RejectsUnknownUser()
    {
        var world = await CreateWorldAsync();
        world.Db.SavedSearches.Add(SavedSearch.Create(Guid.NewGuid(), "بحث", "{}").Value);

        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task ArchiveTransitions_RecordStatusChangeAndLocationInAudit()
    {
        var accessor = new HttpContextAccessor();
        var db = TestContextFactory.Create(new TamperEvidentAuditInterceptor(accessor, TestClock.Instance));
        TestContextFactory.SeedAuditLookups(db);
        var world = await CreateWorldAsync(db);

        // Only a user action produces audit rows, so the caller signs in after the seeding above.
        accessor.HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, world.Author.UserId.ToString()),
                new Claim("dept_id", world.Author.PrimaryDepartmentId.ToString())
            }, authenticationType: "Test"))
        };

        var document = await ArchivedDocumentAsync(world, world.Parent.DepartmentId);

        var archived = await db.AuditLogs.SingleAsync(log =>
            log.ModuleName == "ARCHIVING" && log.EntityId == document.DocumentId.ToString());
        Assert.Contains($"\"statusId\":{DocumentStatusType.ArchivedId}", archived.NewValuesJson);
        Assert.Contains($"\"archiveLocationId\":{ArchiveLocation.PhysicalArchiveId}", archived.NewValuesJson);
        Assert.Contains($"\"statusId\":{DocumentStatusType.RegisteredId}", archived.OldValuesJson);

        Assert.True(document.Unarchive(world.Author.UserId, "سبب").IsSuccess);
        await db.SaveChangesAsync();

        var restored = (await db.AuditLogs
                .Where(log => log.ModuleName == "ARCHIVING" && log.EntityId == document.DocumentId.ToString())
                .OrderBy(log => log.AuditLogId)
                .ToListAsync())
            .Last();
        Assert.Contains($"\"statusId\":{DocumentStatusType.RegisteredId}", restored.NewValuesJson);
        Assert.Contains($"\"statusId\":{DocumentStatusType.ArchivedId}", restored.OldValuesJson);
    }
}
