using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DriveSpaceRepository : IDriveSpaceRepository
{
    private readonly DiwanDbContext _context;

    public DriveSpaceRepository(DiwanDbContext context) => _context = context;

    public Task<DriveSpace?> GetByIdAsync(Guid spaceId, CancellationToken cancellationToken = default) =>
        _context.DriveSpaces.FirstOrDefaultAsync(space => space.SpaceId == spaceId, cancellationToken);

    public Task<DriveSpace?> GetPersonalByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
        _context.DriveSpaces.FirstOrDefaultAsync(
            space => space.OwnerUserId == ownerUserId && space.SpaceTypeId == DriveSpaceType.PersonalId,
            cancellationToken);

    public async Task<IReadOnlyList<DriveSpace>> ListByOwnerAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
        await _context.DriveSpaces
            .Where(space => space.OwnerUserId == ownerUserId)
            .OrderBy(space => space.SpaceTypeId)
            .ThenBy(space => space.Name)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DriveSpace>> ListSharedForUserAsync(
        Guid userId,
        IReadOnlyCollection<Guid> departmentIds,
        CancellationToken cancellationToken = default) =>
        await _context.DriveSpaces
            .Where(space =>
                space.SpaceTypeId == DriveSpaceType.SharedId &&
                space.TrashedAt == null &&
                (space.OwnerUserId == userId ||
                 _context.DriveItems.Any(item =>
                    item.SpaceId == space.SpaceId &&
                    _context.DrivePermissions.Any(grant =>
                        grant.ItemId == item.ItemId &&
                        ((grant.PrincipalTypeId == DrivePrincipalType.UserId && grant.PrincipalId == userId) ||
                         (grant.PrincipalTypeId == DrivePrincipalType.DepartmentId && departmentIds.Contains(grant.PrincipalId)))))))
            .OrderBy(space => space.Name)
            .ToListAsync(cancellationToken);

    public void Add(DriveSpace space) => _context.DriveSpaces.Add(space);
}

internal sealed class DriveItemRepository : IDriveItemRepository
{
    private readonly DiwanDbContext _context;

    public DriveItemRepository(DiwanDbContext context) => _context = context;

    public Task<DriveItem?> GetByIdAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        _context.DriveItems.FirstOrDefaultAsync(item => item.ItemId == itemId, cancellationToken);

    public async Task<IReadOnlyList<DriveItem>> GetChildrenAsync(
        Guid spaceId,
        Guid? parentId,
        CancellationToken cancellationToken = default) =>
        await _context.DriveItems
            .Where(item => item.SpaceId == spaceId && item.ParentId == parentId && item.TrashedAt == null)
            .OrderBy(item => item.ItemKindId)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> NameExistsAsync(
        Guid spaceId,
        Guid? parentId,
        string name,
        Guid? excludeItemId = null,
        CancellationToken cancellationToken = default) =>
        _context.DriveItems.AnyAsync(
            item => item.SpaceId == spaceId &&
                    item.ParentId == parentId &&
                    item.Name == name &&
                    item.TrashedAt == null &&
                    (excludeItemId == null || item.ItemId != excludeItemId),
            cancellationToken);

    public Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        _context.GetAncestorIdsAsync(itemId, cancellationToken);

    public async Task<int> GetDepthAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        (await _context.GetAncestorIdsAsync(itemId, cancellationToken)).Count + 1;

    public async Task<PagedList<DriveItem>> ListPagedAsync(
        PageRequest page,
        DriveItemFilter filter,
        Guid callerUserId,
        IReadOnlyCollection<Guid> callerDepartmentIds,
        CancellationToken cancellationToken = default)
    {
        var isAdmin = await _context.UserRoles
            .AnyAsync(link => link.UserId == callerUserId &&
                              _context.Roles.Any(role => role.RoleId == link.RoleId && role.Code == "ADMIN"),
                cancellationToken);

        var maxDepth = filter.Trashed ? 0 : await _context.GetMaxDepthAsync(cancellationToken);

        var query = _context.DriveItems.AsQueryable();

        if (filter.SpaceId is { } spaceId)
            query = query.Where(item => item.SpaceId == spaceId);
        if (filter.ParentId is { } parentId)
            query = query.Where(item => item.ParentId == parentId);
        if (filter.ItemKindId is { } kindId)
            query = query.Where(item => item.ItemKindId == kindId);
        if (filter.OwnerUserId is { } ownerUserId)
            query = query.Where(item => item.OwnerUserId == ownerUserId);

        query = filter.Trashed
            ? query.Where(item => item.TrashedAt != null)
            : query.Where(item => item.TrashedAt == null);

        if (filter.ModifiedFrom is { } from)
            query = query.Where(item => (item.UpdatedAt ?? item.CreatedAt) >= from);
        if (filter.ModifiedTo is { } to)
            query = query.Where(item => (item.UpdatedAt ?? item.CreatedAt) <= to);

        if (filter.StarredOnly)
            query = query.Where(item => _context.DriveStars.Any(star => star.UserId == callerUserId && star.ItemId == item.ItemId));

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search.Trim()}%";
            query = query.Where(item => EF.Functions.Like(item.Name, pattern));
        }

        query = query.WhereAccessibleBy(_context, callerUserId, callerDepartmentIds, isAdmin, filter.Trashed, maxDepth);

        query = (page.NormalizedSortBy, page.SortDescending) switch
        {
            ("size", true) => query.OrderByDescending(item => item.SizeBytes),
            ("size", false) => query.OrderBy(item => item.SizeBytes),
            ("modified", true) => query.OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt),
            ("modified", false) => query.OrderBy(item => item.UpdatedAt ?? item.CreatedAt),
            ("created", true) => query.OrderByDescending(item => item.CreatedAt),
            ("created", false) => query.OrderBy(item => item.CreatedAt),
            ("name", true) => query.OrderByDescending(item => item.Name),
            ("name", false) => query.OrderBy(item => item.Name),
            // Default: folders first, then name ascending.
            (_, true) => query.OrderByDescending(item => item.ItemKindId).ThenByDescending(item => item.Name),
            (_, false) => query.OrderBy(item => item.ItemKindId).ThenBy(item => item.Name)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PagedList<DriveItem>(items, totalCount);
    }

    public async Task<IReadOnlyList<DriveItem>> ListTrashExpiredAsync(
        DateTime olderThanUtc,
        int take,
        CancellationToken cancellationToken = default) =>
        await _context.DriveItems
            .Where(item => item.TrashedAt != null && item.TrashedAt < olderThanUtc)
            .OrderBy(item => item.TrashedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(DriveItem item) => _context.DriveItems.Add(item);
}

internal sealed class DriveFileVersionRepository : IDriveFileVersionRepository
{
    private readonly DiwanDbContext _context;

    public DriveFileVersionRepository(DiwanDbContext context) => _context = context;

    public Task<DriveFileVersion?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default) =>
        _context.DriveFileVersions.FirstOrDefaultAsync(version => version.VersionId == versionId, cancellationToken);

    public async Task<IReadOnlyList<DriveFileVersion>> ListByItemAsync(Guid fileItemId, CancellationToken cancellationToken = default) =>
        await _context.DriveFileVersions
            .Where(version => version.FileItemId == fileItemId)
            .OrderByDescending(version => version.VersionNumber)
            .ToListAsync(cancellationToken);

    public async Task<int> GetNextVersionNumberAsync(Guid fileItemId, CancellationToken cancellationToken = default)
    {
        var current = await _context.DriveFileVersions
            .Where(version => version.FileItemId == fileItemId)
            .MaxAsync(version => (int?)version.VersionNumber, cancellationToken);
        return (current ?? 0) + 1;
    }

    public async Task<IReadOnlyList<DriveFileVersion>> ListPendingOlderThanAsync(
        DateTime olderThanUtc,
        int take,
        CancellationToken cancellationToken = default) =>
        await _context.DriveFileVersions
            .Where(version => version.StatusId == DriveVersionStatus.PendingId && version.CreatedAt < olderThanUtc)
            .OrderBy(version => version.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(DriveFileVersion version) => _context.DriveFileVersions.Add(version);
}

internal sealed class DrivePermissionRepository : IDrivePermissionRepository
{
    private readonly DiwanDbContext _context;

    public DrivePermissionRepository(DiwanDbContext context) => _context = context;

    public Task<DrivePermission?> GetByIdAsync(Guid permissionId, CancellationToken cancellationToken = default) =>
        _context.DrivePermissions.FirstOrDefaultAsync(permission => permission.PermissionId == permissionId, cancellationToken);

    public async Task<IReadOnlyList<DrivePermission>> ListByItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        await _context.DrivePermissions
            .Where(permission => permission.ItemId == itemId)
            .OrderBy(permission => permission.PrincipalTypeId)
            .ThenBy(permission => permission.GrantedAt)
            .ToListAsync(cancellationToken);

    public Task<DrivePermission?> FindAsync(
        Guid itemId,
        int principalTypeId,
        Guid principalId,
        CancellationToken cancellationToken = default) =>
        _context.DrivePermissions.FirstOrDefaultAsync(
            permission => permission.ItemId == itemId &&
                          permission.PrincipalTypeId == principalTypeId &&
                          permission.PrincipalId == principalId,
            cancellationToken);

    public async Task<IReadOnlyList<DrivePermission>> ListForPrincipalAsync(
        IReadOnlyCollection<Guid> itemIds,
        Guid userId,
        IReadOnlyCollection<Guid> departmentIds,
        CancellationToken cancellationToken = default) =>
        await _context.DrivePermissions
            .Where(permission => itemIds.Contains(permission.ItemId) &&
                                 ((permission.PrincipalTypeId == DrivePrincipalType.UserId && permission.PrincipalId == userId) ||
                                  (permission.PrincipalTypeId == DrivePrincipalType.DepartmentId && departmentIds.Contains(permission.PrincipalId))))
            .ToListAsync(cancellationToken);

    public void Add(DrivePermission permission) => _context.DrivePermissions.Add(permission);

    public void Remove(DrivePermission permission) => _context.DrivePermissions.Remove(permission);
}

internal sealed class DriveDeletionRequestRepository : IDriveDeletionRequestRepository
{
    private readonly DiwanDbContext _context;

    public DriveDeletionRequestRepository(DiwanDbContext context) => _context = context;

    public Task<DriveDeletionRequest?> GetByIdAsync(Guid requestId, CancellationToken cancellationToken = default) =>
        _context.DriveDeletionRequests.FirstOrDefaultAsync(request => request.RequestId == requestId, cancellationToken);

    public Task<DriveDeletionRequest?> GetPendingByItemAsync(Guid itemId, CancellationToken cancellationToken = default) =>
        _context.DriveDeletionRequests.FirstOrDefaultAsync(
            request => request.ItemId == itemId && request.StatusId == DriveDeletionRequestStatus.PendingId,
            cancellationToken);

    public async Task<PagedList<DriveDeletionRequest>> ListPagedAsync(
        PageRequest page,
        int? statusId,
        Guid? requestedByUserId = null,
        Guid? reviewerUserId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DriveDeletionRequests.AsQueryable();

        if (statusId is { } status)
            query = query.Where(request => request.StatusId == status);
        if (requestedByUserId is { } requester)
            query = query.Where(request => request.RequestedByUserId == requester);
        if (reviewerUserId is { } reviewer)
        {
            // Requests the reviewer may approve: the item owner's active department head is the
            // reviewer (mirrors DriveAccessEvaluator.GetDeletionApproverAsync). ADMIN passes null.
            var reviewableItemIds =
                from item in _context.DriveItems
                join owner in _context.Users.Where(user => user.IsActive)
                    on item.OwnerUserId equals owner.UserId
                join department in _context.Departments.Where(dept => dept.IsActive && dept.ManagerEmployeeId != null)
                    on owner.PrimaryDepartmentId equals department.DepartmentId
                join head in _context.Users.Where(user => user.IsActive && user.EmployeeId != null)
                    on department.ManagerEmployeeId equals head.EmployeeId
                where head.UserId == reviewer
                select item.ItemId;
            query = query.Where(request => reviewableItemIds.Contains(request.ItemId));
        }

        query = query.OrderByDescending(request => request.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PagedList<DriveDeletionRequest>(items, totalCount);
    }

    public void Add(DriveDeletionRequest request) => _context.DriveDeletionRequests.Add(request);
}

internal sealed class DriveEditSessionRepository : IDriveEditSessionRepository
{
    private readonly DiwanDbContext _context;

    public DriveEditSessionRepository(DiwanDbContext context) => _context = context;

    public Task<DriveEditSession?> GetByIdAsync(Guid sessionId, CancellationToken cancellationToken = default) =>
        _context.DriveEditSessions.FirstOrDefaultAsync(session => session.SessionId == sessionId, cancellationToken);

    public async Task<IReadOnlyList<DriveEditSession>> ListOpenByItemAsync(Guid fileItemId, CancellationToken cancellationToken = default) =>
        await _context.DriveEditSessions
            .Where(session => session.FileItemId == fileItemId && session.StatusId == DriveEditSessionStatus.OpenId)
            .OrderBy(session => session.OpenedAt)
            .ToListAsync(cancellationToken);

    public Task<DriveEditSession?> FindOpenByKeyAsync(string documentKey, CancellationToken cancellationToken = default) =>
        _context.DriveEditSessions.FirstOrDefaultAsync(
            session => session.DocumentKey == documentKey && session.StatusId == DriveEditSessionStatus.OpenId,
            cancellationToken);

    public async Task<IReadOnlyList<DriveEditSession>> ListIdleOpenAsync(
        DateTime idleSinceUtc,
        int take,
        CancellationToken cancellationToken = default) =>
        await _context.DriveEditSessions
            .Where(session => session.StatusId == DriveEditSessionStatus.OpenId && session.LastActivityAt < idleSinceUtc)
            .OrderBy(session => session.LastActivityAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public void Add(DriveEditSession session) => _context.DriveEditSessions.Add(session);
}

internal sealed class DriveStarRepository : IDriveStarRepository
{
    private readonly DiwanDbContext _context;

    public DriveStarRepository(DiwanDbContext context) => _context = context;

    public Task<DriveStar?> FindAsync(Guid userId, Guid itemId, CancellationToken cancellationToken = default) =>
        _context.DriveStars.FirstOrDefaultAsync(star => star.UserId == userId && star.ItemId == itemId, cancellationToken);

    public async Task<IReadOnlyList<DriveStar>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.DriveStars
            .Where(star => star.UserId == userId)
            .OrderByDescending(star => star.CreatedAt)
            .ToListAsync(cancellationToken);

    public void Add(DriveStar star) => _context.DriveStars.Add(star);

    public void Remove(DriveStar star) => _context.DriveStars.Remove(star);
}