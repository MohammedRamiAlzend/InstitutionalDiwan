using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Authorization;

/// <summary>
/// Effective Drive access for the ambient caller (Module 11), in order: ADMIN (or DRIVE_MANAGE)
/// manages everything; the space owner manages their whole space; otherwise the strongest matching
/// grant — user directly or one of the caller's active departments — found walking from the item
/// up to the root, stopping after the first node with broken inheritance. Trashed items are
/// reachable only by their owner, the space owner, ADMIN, or the owner's department head, and
/// live items under a trashed ancestor are unreachable by anyone but ADMIN.
/// <c>DriveItemAccessQuery</c> is the list-query counterpart; persistence tests hold them equal.
/// </summary>
public sealed class DriveAccessEvaluator : IDriveAccessEvaluator
{
    private readonly IUserContext _userContext;
    private readonly IDriveItemRepository _itemRepository;
    private readonly IDriveSpaceRepository _spaceRepository;
    private readonly IDrivePermissionRepository _permissionRepository;
    private readonly DiwanDbContext _context;

    public DriveAccessEvaluator(
        IUserContext userContext,
        IDriveItemRepository itemRepository,
        IDriveSpaceRepository spaceRepository,
        IDrivePermissionRepository permissionRepository,
        DiwanDbContext context)
    {
        _userContext = userContext;
        _itemRepository = itemRepository;
        _spaceRepository = spaceRepository;
        _permissionRepository = permissionRepository;
        _context = context;
    }

    public async Task<int> GetEffectiveRoleAsync(Guid itemId, CancellationToken cancellationToken = default)
    {
        if (!_userContext.IsAuthenticated || itemId == Guid.Empty)
            return 0;

        var item = await _itemRepository.GetByIdAsync(itemId, cancellationToken);
        if (item is null)
            return 0;

        if (IsAdmin)
            return DriveRole.ManagerId;

        var space = await _spaceRepository.GetByIdAsync(item.SpaceId, cancellationToken);
        if (space is not null && space.OwnerUserId == _userContext.UserId)
            return DriveRole.ManagerId;

        if (item.IsTrashed)
        {
            return item.OwnerUserId == _userContext.UserId ||
                   await IsDepartmentHeadOfAsync(_userContext.UserId, item.OwnerUserId, cancellationToken)
                ? DriveRole.ManagerId
                : 0;
        }

        var chain = new List<Guid> { item.ItemId };
        var ancestorIds = await _itemRepository.GetAncestorIdsAsync(item.ItemId, cancellationToken);
        if (ancestorIds.Count > 0)
        {
            var ancestors = await _context.DriveItems
                .Where(node => ancestorIds.Contains(node.ItemId))
                .Select(node => new { node.ItemId, node.BreakInheritance, node.TrashedAt })
                .ToListAsync(cancellationToken);

            // Trashing a folder marks only its root; everything below it is unreachable live,
            // regardless of grants or inheritance breaks (mirrors DriveItemAccessQuery).
            if (ancestors.Any(node => node.TrashedAt != null))
                return 0;

            if (!item.BreakInheritance)
            {
                var byId = ancestors.ToDictionary(node => node.ItemId);
                foreach (var ancestorId in ancestorIds)
                {
                    chain.Add(ancestorId);
                    if (byId.TryGetValue(ancestorId, out var node) && node.BreakInheritance)
                        break;
                }
            }
        }

        var departmentIds = await GetCallerDepartmentIdsAsync(cancellationToken);
        var grants = await _permissionRepository.ListForPrincipalAsync(
            chain, _userContext.UserId, departmentIds, cancellationToken);

        return grants.Count == 0 ? 0 : grants.Max(grant => grant.RoleId);
    }

    public async Task<bool> CanAccessItemAsync(Guid itemId, int requiredRoleId, CancellationToken cancellationToken = default)
    {
        if (!DriveRole.IsValid(requiredRoleId))
            return false;

        return await GetEffectiveRoleAsync(itemId, cancellationToken) >= requiredRoleId;
    }

    public async Task<Guid?> GetDeletionApproverAsync(Guid ownerUserId, CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
            return null;

        return await _context.Users
            .Where(owner => owner.UserId == ownerUserId && owner.IsActive)
            .Join(_context.Departments.Where(department => department.IsActive && department.ManagerEmployeeId != null),
                owner => owner.PrimaryDepartmentId,
                department => department.DepartmentId,
                (owner, department) => department.ManagerEmployeeId)
            .Join(_context.Users.Where(head => head.IsActive && head.EmployeeId != null),
                managerEmployeeId => managerEmployeeId,
                head => head.EmployeeId,
                (managerEmployeeId, head) => (Guid?)head.UserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> IsDepartmentHeadOfAsync(Guid headUserId, Guid memberUserId, CancellationToken cancellationToken = default)
    {
        if (headUserId == Guid.Empty || memberUserId == Guid.Empty)
            return false;

        var approver = await GetDeletionApproverAsync(memberUserId, cancellationToken);
        return approver == headUserId;
    }

    public bool IsAdmin =>
        _userContext.Roles.Contains("ADMIN") ||
        _userContext.Permissions.Contains(Permissions.DriveManage);

    /// <summary>The caller's primary department plus active assignments (membership, not scope subtree).</summary>
    public async Task<IReadOnlyCollection<Guid>> GetCallerDepartmentIdsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var assigned = await _context.UserDepartmentAssignments
            .Where(assignment => assignment.UserId == _userContext.UserId &&
                                 assignment.IsActive &&
                                 assignment.StartDate <= now &&
                                 (assignment.EndDate == null || assignment.EndDate > now))
            .Select(assignment => assignment.DepartmentId)
            .ToListAsync(cancellationToken);

        var departments = new HashSet<Guid>(assigned);
        if (_userContext.PrimaryDepartmentId != Guid.Empty)
            departments.Add(_userContext.PrimaryDepartmentId);

        return departments;
    }
}