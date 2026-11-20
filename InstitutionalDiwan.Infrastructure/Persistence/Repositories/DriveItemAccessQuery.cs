using InstitutionalDiwan.Domain.Entities.Drive;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

/// <summary>
/// The Drive effective-access rule as SQL, mirroring <c>DriveAccessEvaluator</c>: ADMIN sees
/// everything; a space owner sees their whole space; everyone else sees items with a matching
/// grant (user or department) on the item or an ancestor, walking up until a node with broken
/// inheritance (that node's own grants still count). Live browsing also hides anything under a
/// trashed ancestor, because trashing a folder marks only its root. The ancestor walk is a
/// bounded join chain — one level per actual tree level — so it translates on SQL Server and
/// SQLite alike. Persistence tests hold this predicate and the evaluator equal.
/// </summary>
internal static class DriveItemAccessQuery
{
    public static IQueryable<DriveItem> WhereAccessibleBy(
        this IQueryable<DriveItem> items,
        DiwanDbContext context,
        Guid callerUserId,
        IReadOnlyCollection<Guid> callerDepartmentIds,
        bool isAdmin,
        bool trashed,
        int maxDepth)
    {
        if (isAdmin)
            return items;

        var ownedSpaceIds = context.DriveSpaces
            .Where(space => space.OwnerUserId == callerUserId)
            .Select(space => space.SpaceId);

        if (trashed)
        {
            // Trash is visible to the item's owner, the space's owner, and the owner's department head.
            return items.Where(item =>
                item.OwnerUserId == callerUserId ||
                ownedSpaceIds.Contains(item.SpaceId) ||
                context.Users.Any(owner =>
                    owner.UserId == item.OwnerUserId &&
                    context.Departments.Any(department =>
                        department.DepartmentId == owner.PrimaryDepartmentId &&
                        department.IsActive &&
                        department.ManagerEmployeeId != null &&
                        context.Users.Any(head =>
                            head.UserId == callerUserId &&
                            head.IsActive &&
                            head.EmployeeId == department.ManagerEmployeeId))));
        }

        var matchingGrants = context.DrivePermissions.Where(grant =>
            (grant.PrincipalTypeId == DrivePrincipalType.UserId && grant.PrincipalId == callerUserId) ||
            (grant.PrincipalTypeId == DrivePrincipalType.DepartmentId && callerDepartmentIds.Contains(grant.PrincipalId)));

        var empty = context.DriveItems.Where(item => false).Select(item => item.ItemId);
        var grantReachable = empty;
        var trashedSubtree = empty;

        // One chain walking every item up to the root. GrantBlocked records that a break was passed
        // strictly below the current node, so grants above a break are ignored while the break
        // node's own grants still count; the trash walk ignores breaks entirely.
        var chain = context.DriveItems.Select(item => new
        {
            Leaf = item.ItemId,
            NodeId = item.ItemId,
            ParentId = item.ParentId,
            NodeBreak = item.BreakInheritance,
            NodeTrashed = item.TrashedAt != null,
            GrantBlocked = false
        });

        for (var level = 0; level < maxDepth; level++)
        {
            grantReachable = grantReachable.Union(
                chain.Where(node => !node.GrantBlocked && matchingGrants.Any(grant => grant.ItemId == node.NodeId))
                     .Select(node => node.Leaf));
            trashedSubtree = trashedSubtree.Union(
                chain.Where(node => node.NodeTrashed).Select(node => node.Leaf));

            chain = chain
                .Where(node => node.ParentId != null)
                .Join(context.DriveItems,
                    node => node.ParentId,
                    parent => parent.ItemId,
                    (node, parent) => new
                    {
                        Leaf = node.Leaf,
                        NodeId = parent.ItemId,
                        ParentId = parent.ParentId,
                        NodeBreak = parent.BreakInheritance,
                        NodeTrashed = parent.TrashedAt != null,
                        GrantBlocked = node.GrantBlocked || node.NodeBreak
                    });
        }

        return items.Where(item =>
            !trashedSubtree.Contains(item.ItemId) &&
            (ownedSpaceIds.Contains(item.SpaceId) || grantReachable.Contains(item.ItemId)));
    }
}