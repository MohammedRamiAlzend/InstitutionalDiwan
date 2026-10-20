using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Entities.Relations;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core implementation of <see cref="ILookupRepository"/>. The slug-to-entity mapping
/// lives in <see cref="LookupCatalog"/>; this class only coordinates tracking and saves.
/// </summary>
internal sealed class LookupRepository : ILookupRepository
{
    private readonly DiwanDbContext _context;

    public LookupRepository(DiwanDbContext context) => _context = context;

    public bool IsSupported(string lookupKey) => LookupCatalog.TryGet(lookupKey, out _);

    /// <summary>Returns all supported lookup catalog keys.</summary>
    public IReadOnlyList<string> GetSupportedCatalogs() => LookupCatalog.GetSupportedCatalogKeys();

    public async Task<IReadOnlyList<LookupEntity>> ListAsync(
        string lookupKey,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        if (!LookupCatalog.TryGet(lookupKey, out var catalog))
            return Array.Empty<LookupEntity>();

        return await catalog.ListAsync(_context, includeInactive, cancellationToken);
    }

    public async Task<LookupEntity?> GetByIdAsync(
        string lookupKey,
        int id,
        CancellationToken cancellationToken = default)
    {
        if (!LookupCatalog.TryGet(lookupKey, out var catalog))
            return null;

        return await catalog.GetByIdAsync(_context, id, cancellationToken);
    }

    public async Task<Result<LookupEntity>> AddAsync(
        string lookupKey,
        int id,
        string desc,
        string descAr,
        CancellationToken cancellationToken = default)
    {
        if (!LookupCatalog.TryGet(lookupKey, out var catalog))
            return Result.Failure<LookupEntity>(DomainErrors.Lookup.UnknownCatalog(lookupKey));

        if (await catalog.GetByIdAsync(_context, id, cancellationToken) is not null)
            return Result.Failure<LookupEntity>(DomainErrors.Lookup.IdAlreadyExists(lookupKey, id));

        var created = await catalog.CreateAsync(_context, id, desc, descAr, cancellationToken);
        if (created.IsFailure)
            return created;

        _context.Add(created.Value);
        return created;
    }

    public async Task<LookupEntity?> RelabelAsync(
        string lookupKey,
        int id,
        string desc,
        string descAr,
        CancellationToken cancellationToken = default)
    {
        var entry = await GetByIdAsync(lookupKey, id, cancellationToken);
        if (entry is null)
            return null;

        entry.Relabel(desc, descAr);
        return entry;
    }

    public async Task<LookupEntity?> SetActiveStateAsync(
        string lookupKey,
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        var entry = await GetByIdAsync(lookupKey, id, cancellationToken);
        if (entry is null)
            return null;

        if (isActive)
            entry.Activate();
        else
            entry.Deactivate();

        return entry;
    }

    public async Task<bool> IsInUseAsync(
        string lookupKey,
        int id,
        CancellationToken cancellationToken = default)
    {
        if (!LookupCatalog.TryGet(lookupKey, out var catalog))
            return false;

        var principal = _context.Model.FindEntityType(catalog.EntityType);
        if (principal is null)
            return false;

        // Every FK in the model that points at this lookup table is a usage. The reference is
        // resolved from EF metadata so a new dependent table automatically participates.
        foreach (var foreignKey in _context.Model.GetEntityTypes().SelectMany(type => type.GetForeignKeys()))
        {
            if (!ReferenceEquals(foreignKey.PrincipalEntityType, principal))
                continue;

            if (foreignKey.Properties.Count != 1)
                continue;

            var property = foreignKey.Properties[0];
            var isNullable = property.ClrType == typeof(int?);
            if (property.ClrType != typeof(int) && !isNullable)
                continue;

            if (await HasReferenceAsync(
                    _context,
                    foreignKey.DeclaringEntityType.ClrType,
                    property.Name,
                    id,
                    isNullable,
                    cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<bool> HasReferenceAsync(
        DiwanDbContext context,
        Type entityType,
        string propertyName,
        int id,
        bool isNullable,
        CancellationToken cancellationToken)
    {
        var methodName = isNullable
            ? nameof(HasNullableReferenceCoreAsync)
            : nameof(HasReferenceCoreAsync);

        var method = typeof(LookupRepository)
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(entityType);

        var task = (Task<bool>)method.Invoke(
            null,
            new object[] { context, propertyName, id, cancellationToken })!;

        return await task;
    }

    private static Task<bool> HasReferenceCoreAsync<TEntity>(
        DiwanDbContext context,
        string propertyName,
        int id,
        CancellationToken cancellationToken)
        where TEntity : class
        => context.Set<TEntity>().AnyAsync(
            entity => EF.Property<int>(entity, propertyName) == id,
            cancellationToken);

    private static Task<bool> HasNullableReferenceCoreAsync<TEntity>(
        DiwanDbContext context,
        string propertyName,
        int id,
        CancellationToken cancellationToken)
        where TEntity : class
        => context.Set<TEntity>().AnyAsync(
            entity => EF.Property<int?>(entity, propertyName) == id,
            cancellationToken);
}

/// <summary>Creates and tracks one lookup entry; returns a domain failure for invalid labels.</summary>
public delegate Task<Result<LookupEntity>> LookupEntryFactory(
    DiwanDbContext context,
    int id,
    string desc,
    string descAr,
    CancellationToken cancellationToken);

/// <summary>Operation set for one catalog slug.</summary>
public sealed class LookupDefinition
{
    public LookupDefinition(
        Type entityType,
        LookupEntryFactory createAsync,
        Func<DiwanDbContext, bool, CancellationToken, Task<IReadOnlyList<LookupEntity>>> listAsync,
        Func<DiwanDbContext, int, CancellationToken, Task<LookupEntity?>> getByIdAsync)
    {
        EntityType = entityType;
        CreateAsync = createAsync;
        ListAsync = listAsync;
        GetByIdAsync = getByIdAsync;
    }

    public Type EntityType { get; }

    public LookupEntryFactory CreateAsync { get; }

    public Func<DiwanDbContext, bool, CancellationToken, Task<IReadOnlyList<LookupEntity>>> ListAsync { get; }

    public Func<DiwanDbContext, int, CancellationToken, Task<LookupEntity?>> GetByIdAsync { get; }
}

/// <summary>
/// Deterministic route-segment registry: every supported <c>/api/lookups/{lookup}</c> slug maps to
/// exactly one seeded lookup entity. Adding a lookup to the API means adding one entry here.
/// </summary>
public static class LookupCatalog
{
    public static readonly Dictionary<string, LookupDefinition> Definitions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Module 01 — Organization
            ["department-types"] = DefineDepartmentTypes(),
            ["numbering-modes"] = Define(NumberingMode.Create),

            // Module 02 — Identity & access
            ["user-scope-types"] = Define(UserScopeType.Create),
            ["assignment-types"] = Define(AssignmentType.Create),
            ["access-levels"] = Define(AccessLevel.Create),

            // Module 03 — Correspondence
            ["document-directions"] = Define(DocumentDirection.Create),
            ["document-types"] = Define<DocumentType>((id, desc, descAr) => DocumentType.Create(id, desc, descAr)),
            ["document-status-types"] = Define<DocumentStatusType>((id, desc, descAr) => DocumentStatusType.Create(id, desc, descAr)),
            ["document-confidentiality-levels"] = Define(DocumentConfidentialityLevel.Create),
            ["document-priority-levels"] = Define(DocumentPriorityLevel.Create),
            ["participant-roles"] = Define(ParticipantRole.Create),
            ["participant-target-types"] = Define(ParticipantTargetType.Create),
            ["organization-types"] = Define(OrganizationType.Create),

            // Module 04 — Numbering
            ["document-number-statuses"] = Define(DocumentNumberStatus.Create),
            ["document-number-event-types"] = Define(DocumentNumberEventType.Create),
            ["numbering-reset-policies"] = Define(NumberingResetPolicy.Create),
            ["document-number-scopes"] = Define(DocumentNumberScope.Create),

            // Module 05 — Movements
            ["movement-types"] = Define(MovementType.Create),
            ["movement-statuses"] = Define(MovementStatus.Create),
            ["movement-step-statuses"] = Define(MovementStepStatus.Create),
            ["movement-event-types"] = Define(MovementEventType.Create),

            // Module 06 — Attachments
            ["storage-provider-types"] = Define(StorageProviderType.Create),
            ["document-file-types"] = Define<DocumentFileType>((id, desc, descAr) => DocumentFileType.Create(id, desc, descAr)),
            ["file-storage-providers"] = ReadOnly<FileStorageProvider>("file-storage-providers"),

            // Module 07 — Archiving
            ["archive-event-types"] = Define(ArchiveEventType.Create),
            ["archive-locations"] = Define(ArchiveLocation.Create),

            // Module 08 — Relations
            ["document-relation-types"] = Define<DocumentRelationType>((id, desc, descAr) => DocumentRelationType.Create(id, desc, descAr)),
            ["relation-action-types"] = Define(RelationActionType.Create),

            // Module 09 — Audit
            ["audit-action-types"] = Define(AuditActionType.Create),
            ["audit-entity-types"] = Define(AuditEntityType.Create),

            // Module 10 — Notifications
            ["notification-types"] = ReadOnly<NotificationType>("notification-types"),
            ["notification-priorities"] = Define(NotificationPriority.Create),
            ["notification-channels"] = Define(NotificationChannel.Create),
            ["notification-outbox-statuses"] = Define(NotificationOutboxStatus.Create),
        };

    public static bool TryGet(string lookupKey, out LookupDefinition definition) =>
        Definitions.TryGetValue(lookupKey?.Trim() ?? string.Empty, out definition!);

    /// <summary>Returns all supported lookup catalog keys.</summary>
    public static IReadOnlyList<string> GetSupportedCatalogKeys() => Definitions.Keys.ToList();

    private static LookupDefinition Define<TEntity>(
        Func<int, string?, string?, Result<TEntity>> create)
        where TEntity : LookupEntity
        => Define<TEntity>((_, id, desc, descAr, _) => Task.FromResult(Map(create(id, desc, descAr))));

    private static LookupDefinition Define<TEntity>(LookupEntryFactory createAsync)
        where TEntity : LookupEntity
        => new(
            typeof(TEntity),
            createAsync,
            (context, includeInactive, cancellationToken) =>
                ListCoreAsync<TEntity>(context, includeInactive, cancellationToken),
            (context, id, cancellationToken) =>
                GetCoreAsync<TEntity>(context, id, cancellationToken));

    private static LookupDefinition DefineDepartmentTypes() =>
        Define<DepartmentType>(async (context, id, desc, descAr, cancellationToken) =>
        {
            var currentMax = await context.DepartmentTypes
                .MaxAsync(type => (int?)type.HierarchyLevelOrder, cancellationToken) ?? 0;

            return Map(DepartmentType.Create(id, desc, descAr, currentMax + 1));
        });

    /// <summary>
    /// Catalogs whose entities need more than a label to be created are managed by their own
    /// module (attachments / notifications); they remain readable, relabellable and
    /// activatable/deactivatable through this API.
    /// </summary>
    private static LookupDefinition ReadOnly<TEntity>(string slug)
        where TEntity : LookupEntity
        => new(
            typeof(TEntity),
            (_, _, _, _, _) => Task.FromResult(
                Result.Failure<LookupEntity>(DomainErrors.Lookup.CatalogReadOnly(slug))),
            (context, includeInactive, cancellationToken) =>
                ListCoreAsync<TEntity>(context, includeInactive, cancellationToken),
            (context, id, cancellationToken) => GetCoreAsync<TEntity>(context, id, cancellationToken));

    private static async Task<IReadOnlyList<LookupEntity>> ListCoreAsync<TEntity>(
        DiwanDbContext context,
        bool includeInactive,
        CancellationToken cancellationToken)
        where TEntity : LookupEntity
    {
        var query = context.Set<TEntity>().AsNoTracking();
        if (!includeInactive)
            query = query.Where(entry => entry.IsActive);

        var entries = await query.OrderBy(entry => entry.Id).ToListAsync(cancellationToken);
        return entries.Cast<LookupEntity>().ToList();
    }

    private static async Task<LookupEntity?> GetCoreAsync<TEntity>(
        DiwanDbContext context,
        int id,
        CancellationToken cancellationToken)
        where TEntity : LookupEntity
        => await context.Set<TEntity>()
            .FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);

    private static Result<LookupEntity> Map<TEntity>(Result<TEntity> result)
        where TEntity : LookupEntity
        => result.IsSuccess
            ? Result.Success<LookupEntity>(result.Value)
            : Result.Failure<LookupEntity>(result.Error);
}
