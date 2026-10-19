using InstitutionalDiwan.Application.Features.Lookups;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>
/// Exercises the generic reference-lookup API against a real SQLite in-memory database, so the
/// slug registry, EF queries, and the reference-in-use guard are all covered end to end.
/// </summary>
public sealed class LookupHandlerTests
{
    private const string DepartmentTypes = "department-types";
    private const string DocumentTypes = "document-types";

    private static async Task<(DiwanDbContext Db, LookupRepository Repository)> CreateAsync()
    {
        var db = TestContextFactory.Create();
        await SeedSectionTypeAsync(db);
        return (db, new LookupRepository(db));
    }

    private static async Task<DepartmentType> SeedSectionTypeAsync(DiwanDbContext db)
    {
        var type = DepartmentType.Create(DepartmentType.SectionId, "Section", "قسم", hierarchyLevelOrder: 3);
        Assert.True(type.IsSuccess);
        db.DepartmentTypes.Add(type.Value);
        await db.SaveChangesAsync();
        return type.Value;
    }

    [Fact]
    public void IsSupported_RecognisesEverySeededCatalogSlug()
    {
        using var db = TestContextFactory.Create();
        var repository = new LookupRepository(db);

        var slugs = new[]
        {
            "department-types", "numbering-modes",
            "user-scope-types", "assignment-types", "access-levels",
            "document-directions", "document-types", "document-status-types",
            "document-confidentiality-levels", "document-priority-levels",
            "participant-roles", "participant-target-types", "organization-types",
            "document-number-statuses", "document-number-event-types", "numbering-reset-policies",
            "movement-types", "movement-statuses", "movement-step-statuses", "movement-event-types",
            "storage-provider-types", "document-file-types", "file-storage-providers",
            "archive-event-types", "archive-locations",
            "document-relation-types", "relation-action-types",
            "audit-action-types", "audit-entity-types",
            "notification-types", "notification-priorities", "notification-channels",
            "notification-outbox-statuses",
        };

        Assert.Equal(33, slugs.Length);
        Assert.All(slugs, slug => Assert.True(repository.IsSupported(slug), $"'{slug}' should be a supported catalog."));
        Assert.False(repository.IsSupported("not-a-catalog"));
    }

    [Fact]
    public async Task List_UnknownCatalogSlug_ReturnsNotFound()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        var result = await new ListLookupEntriesQueryHandler(repository)
            .Handle(new ListLookupEntriesQuery("unknown-catalog"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Lookup.UnknownCatalog", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task List_ExcludesInactiveEntriesUnlessRequested()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        var unit = DepartmentType.Create(DepartmentType.UnitId, "Unit", "وحدة", hierarchyLevelOrder: 4);
        Assert.True(unit.IsSuccess);
        db.DepartmentTypes.Add(unit.Value);
        await db.SaveChangesAsync();

        var deactivate = await new DeactivateLookupEntryCommandHandler(repository, db)
            .Handle(new DeactivateLookupEntryCommand(DepartmentTypes, DepartmentType.UnitId), CancellationToken.None);
        Assert.True(deactivate.IsSuccess);

        var active = await new ListLookupEntriesQueryHandler(repository)
            .Handle(new ListLookupEntriesQuery(DepartmentTypes), CancellationToken.None);
        Assert.True(active.IsSuccess);
        Assert.DoesNotContain(active.Value, entry => entry.Id == DepartmentType.UnitId);

        var all = await new ListLookupEntriesQueryHandler(repository)
            .Handle(new ListLookupEntriesQuery(DepartmentTypes, IncludeInactive: true), CancellationToken.None);
        Assert.True(all.IsSuccess);
        Assert.Contains(all.Value, entry => entry.Id == DepartmentType.UnitId);
    }

    [Fact]
    public async Task GetById_WhenEntryIsMissing_ReturnsNotFound()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        var result = await new GetLookupEntryByIdQueryHandler(repository)
            .Handle(new GetLookupEntryByIdQuery(DepartmentTypes, 999), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Lookup.EntryNotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Create_WithNewId_PersistsEntry()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        // A document type defaults to the ALL direction, so that lookup row must exist first.
        var allDirection = DocumentDirection.Create(DocumentDirection.AllId, "All", "الكل");
        Assert.True(allDirection.IsSuccess);
        db.DocumentDirections.Add(allDirection.Value);
        await db.SaveChangesAsync();

        var result = await new CreateLookupEntryCommandHandler(repository, db)
            .Handle(new CreateLookupEntryCommand(DocumentTypes, 99, "Minutes", "محضر"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(99, result.Value.Id);
        Assert.Equal("Minutes", result.Value.Desc);
        Assert.True(result.Value.IsActive);

        var stored = await repository.GetByIdAsync(DocumentTypes, 99, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal("محضر", stored!.DescAr);
    }

    [Fact]
    public async Task Create_WithDuplicateId_ReturnsConflict()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        var result = await new CreateLookupEntryCommandHandler(repository, db)
            .Handle(
                new CreateLookupEntryCommand(DepartmentTypes, DepartmentType.SectionId, "Duplicate", "مكرر"),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Lookup.IdAlreadyExists", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Create_OnReadOnlyCatalog_ReturnsValidationError()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        var result = await new CreateLookupEntryCommandHandler(repository, db)
            .Handle(
                new CreateLookupEntryCommand("file-storage-providers", 5, "Network share", "مشاركة شبكة"),
                CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Lookup.CatalogReadOnly", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public async Task Relabel_UpdatesBothLabels()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        var result = await new RelabelLookupEntryCommandHandler(repository, db)
            .Handle(
                new RelabelLookupEntryCommand(DepartmentTypes, DepartmentType.SectionId, "Branch", "فرع"),
                CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Branch", result.Value.Desc);
        Assert.Equal("فرع", result.Value.DescAr);

        var stored = await repository.GetByIdAsync(DepartmentTypes, DepartmentType.SectionId, CancellationToken.None);
        Assert.Equal("Branch", stored!.Desc);
    }

    [Fact]
    public async Task Relabel_WhenEntryIsMissing_ReturnsNotFound()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        var result = await new RelabelLookupEntryCommandHandler(repository, db)
            .Handle(new RelabelLookupEntryCommand(DepartmentTypes, 404, "Missing", "مفقود"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Lookup.EntryNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Deactivate_WhenEntryIsReferenced_ReturnsConflict()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        var department = Department.Create("HR", "الموارد البشرية", DepartmentType.SectionId);
        Assert.True(department.IsSuccess);
        db.Departments.Add(department.Value);
        await db.SaveChangesAsync();

        var result = await new DeactivateLookupEntryCommandHandler(repository, db)
            .Handle(new DeactivateLookupEntryCommand(DepartmentTypes, DepartmentType.SectionId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Lookup.EntryInUse", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task IsInUseAsync_DetectsReferenceFromNullableForeignKey()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        // DOCUMENT_TYPES rows carry a direction FK, and NUMBERING_REGISTRIES references a document
        // type through a *nullable* column: both must be seen as usages.
        var allDirection = DocumentDirection.Create(DocumentDirection.AllId, "All", "الكل");
        Assert.True(allDirection.IsSuccess);
        db.DocumentDirections.Add(allDirection.Value);

        var memo = DocumentType.Create(DocumentType.InternalMemoId, "Internal memo", "مذكرة داخلية");
        Assert.True(memo.IsSuccess);
        db.DocumentTypes.Add(memo.Value);

        var resetPolicy = NumberingResetPolicy.Create(NumberingResetPolicy.YearlyId, "Yearly", "سنوي");
        Assert.True(resetPolicy.IsSuccess);
        db.NumberingResetPolicies.Add(resetPolicy.Value);
        await db.SaveChangesAsync();

        var registry = NumberingRegistry.Create("MEMO", "ترقيم المذكرات", documentTypeId: DocumentType.InternalMemoId);
        Assert.True(registry.IsSuccess);
        db.NumberingRegistries.Add(registry.Value);
        await db.SaveChangesAsync();

        Assert.True(await repository.IsInUseAsync(DocumentTypes, DocumentType.InternalMemoId, CancellationToken.None));
        Assert.False(await repository.IsInUseAsync(DocumentTypes, DocumentType.OutgoingLetterId, CancellationToken.None));
    }

    [Fact]
    public async Task Deactivate_ThenActivate_RoundTripsState()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        var deactivated = await new DeactivateLookupEntryCommandHandler(repository, db)
            .Handle(new DeactivateLookupEntryCommand(DepartmentTypes, DepartmentType.SectionId), CancellationToken.None);
        Assert.True(deactivated.IsSuccess);
        Assert.False(deactivated.Value.IsActive);

        var reactivated = await new ActivateLookupEntryCommandHandler(repository, db)
            .Handle(new ActivateLookupEntryCommand(DepartmentTypes, DepartmentType.SectionId), CancellationToken.None);
        Assert.True(reactivated.IsSuccess);
        Assert.True(reactivated.Value.IsActive);
    }

    [Fact]
    public async Task Activate_WhenEntryIsMissing_ReturnsNotFound()
    {
        var (db, repository) = await CreateAsync();
        using var _ = db;

        var result = await new ActivateLookupEntryCommandHandler(repository, db)
            .Handle(new ActivateLookupEntryCommand(DepartmentTypes, 404), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Lookup.EntryNotFound", result.Error.Code);
    }
}
