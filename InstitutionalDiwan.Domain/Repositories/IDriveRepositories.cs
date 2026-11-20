namespace InstitutionalDiwan.Domain.Repositories;

/// <summary>Filters for browsing Drive items. Null members are not applied.</summary>
public sealed record DriveItemFilter(
    Guid? SpaceId = null,
    Guid? ParentId = null,
    string? Search = null,
    int? ItemKindId = null,
    Guid? OwnerUserId = null,
    bool Trashed = false,
    bool StarredOnly = false,
    DateTime? ModifiedFrom = null,
    DateTime? ModifiedTo = null);

public interface IDriveSpaceRepository
{
    Task<DriveSpace?> GetByIdAsync(Guid spaceId, CancellationToken cancellationToken = default);
    Task<DriveSpace?> GetPersonalByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriveSpace>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default);
    /// <summary>Shared spaces the user owns or holds a grant on (directly or via a department).</summary>
    Task<IReadOnlyList<DriveSpace>> ListSharedForUserAsync(
        Guid userId,
        IReadOnlyCollection<Guid> departmentIds,
        CancellationToken cancellationToken = default);
    void Add(DriveSpace space);
}

public interface IDriveItemRepository
{
    Task<DriveItem?> GetByIdAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriveItem>> GetChildrenAsync(Guid spaceId, Guid? parentId, CancellationToken cancellationToken = default);
    Task<bool> NameExistsAsync(Guid spaceId, Guid? parentId, string name, Guid? excludeItemId = null, CancellationToken cancellationToken = default);
    /// <summary>Ancestor ids from the parent up to the root, for cycle checks and inheritance walks.</summary>
    Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<int> GetDepthAsync(Guid itemId, CancellationToken cancellationToken = default);
    /// <summary>
    /// A page of items matching the filter. Access filtering is applied inside the query by the
    /// infrastructure implementation using the caller's effective-access predicate (Phase 1+).
    /// </summary>
    Task<PagedList<DriveItem>> ListPagedAsync(
        PageRequest page,
        DriveItemFilter filter,
        Guid callerUserId,
        IReadOnlyCollection<Guid> callerDepartmentIds,
        CancellationToken cancellationToken = default);
    /// <summary>Items whose trash retention expired, for the cleanup job.</summary>
    Task<IReadOnlyList<DriveItem>> ListTrashExpiredAsync(DateTime olderThanUtc, int take, CancellationToken cancellationToken = default);
    void Add(DriveItem item);
}

public interface IDriveFileVersionRepository
{
    Task<DriveFileVersion?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriveFileVersion>> ListByItemAsync(Guid fileItemId, CancellationToken cancellationToken = default);
    Task<int> GetNextVersionNumberAsync(Guid fileItemId, CancellationToken cancellationToken = default);
    /// <summary>Pending uploads older than the cutoff, for the abandonment job.</summary>
    Task<IReadOnlyList<DriveFileVersion>> ListPendingOlderThanAsync(DateTime olderThanUtc, int take, CancellationToken cancellationToken = default);
    void Add(DriveFileVersion version);
}

public interface IDrivePermissionRepository
{
    Task<DrivePermission?> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DrivePermission>> ListByItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    Task<DrivePermission?> FindAsync(Guid itemId, int principalTypeId, Guid principalId, CancellationToken cancellationToken = default);
    /// <summary>Grants on the given items matching the user or any of their departments, for the access evaluator.</summary>
    Task<IReadOnlyList<DrivePermission>> ListForPrincipalAsync(
        IReadOnlyCollection<Guid> itemIds,
        Guid userId,
        IReadOnlyCollection<Guid> departmentIds,
        CancellationToken cancellationToken = default);
    void Add(DrivePermission permission);
    void Remove(DrivePermission permission);
}

public interface IDriveDeletionRequestRepository
{
    Task<DriveDeletionRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<DriveDeletionRequest?> GetPendingByItemAsync(Guid itemId, CancellationToken cancellationToken = default);
    /// <summary>A page of requests, optionally by status; reviewers see requests they may review.</summary>
    Task<PagedList<DriveDeletionRequest>> ListPagedAsync(
        PageRequest page,
        int? statusId,
        Guid? requestedByUserId = null,
        Guid? reviewerUserId = null,
        CancellationToken cancellationToken = default);
    void Add(DriveDeletionRequest request);
}

public interface IDriveEditSessionRepository
{
    Task<DriveEditSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriveEditSession>> ListOpenByItemAsync(Guid fileItemId, CancellationToken cancellationToken = default);
    Task<DriveEditSession?> FindOpenByKeyAsync(string documentKey, CancellationToken cancellationToken = default);
    /// <summary>Open sessions idle since the cutoff, for the expiry job.</summary>
    Task<IReadOnlyList<DriveEditSession>> ListIdleOpenAsync(DateTime idleSinceUtc, int take, CancellationToken cancellationToken = default);
    void Add(DriveEditSession session);
}

public interface IDriveStarRepository
{
    Task<DriveStar?> FindAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DriveStar>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    void Add(DriveStar star);
    void Remove(DriveStar star);
}