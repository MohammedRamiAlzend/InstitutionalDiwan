using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Authorization;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Effective Drive access for the ambient caller, and its parity with the SQL browse predicate:
/// for every caller the evaluator's reachable set equals what <c>ListPagedAsync</c> returns.
/// </summary>
public sealed class DriveAccessEvaluatorTests
{
    private static PageRequest AllRows() => new(1, 100, null, null, true);

    // ── Effective role ──────────────────────────────────────────────────────

    [Fact]
    public async Task SpaceOwner_ManagesEveryItemInOwnSpace()
    {
        var world = await DriveTestWorld.CreateAsync();
        var evaluator = world.EvaluatorFor(world.Owner);

        Assert.Equal(DriveRole.ManagerId, await evaluator.GetEffectiveRoleAsync(world.MemberFile.ItemId));
        Assert.Equal(DriveRole.ManagerId, await evaluator.GetEffectiveRoleAsync(world.DeepFile.ItemId));
    }

    [Fact]
    public async Task Admin_ManagesEverywhere()
    {
        var world = await DriveTestWorld.CreateAsync();
        var evaluator = world.EvaluatorFor(world.Admin, isAdmin: true);

        Assert.Equal(DriveRole.ManagerId, await evaluator.GetEffectiveRoleAsync(world.DeepFile.ItemId));
        Assert.Equal(DriveRole.ManagerId, await evaluator.GetEffectiveRoleAsync(world.MemberFile.ItemId));
    }

    [Fact]
    public async Task GrantHolder_GetsStrongestMatchingGrant()
    {
        var world = await DriveTestWorld.CreateAsync();
        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.ViewerId);
        await world.AddGrantAsync(world.ChildFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.EditorId);
        var evaluator = world.EvaluatorFor(world.Member);

        Assert.Equal(DriveRole.ViewerId, await evaluator.GetEffectiveRoleAsync(world.RootFolder.ItemId));
        Assert.Equal(DriveRole.EditorId, await evaluator.GetEffectiveRoleAsync(world.ChildFolder.ItemId));
        Assert.Equal(DriveRole.EditorId, await evaluator.GetEffectiveRoleAsync(world.DeepFile.ItemId));
        Assert.Equal(0, await evaluator.GetEffectiveRoleAsync(world.MemberFile.ItemId));
    }

    [Fact]
    public async Task DepartmentGrant_AppliesToDepartmentMembers()
    {
        var world = await DriveTestWorld.CreateAsync();
        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.DepartmentId, world.Dept.DepartmentId, DriveRole.EditorId);

        Assert.Equal(DriveRole.EditorId, await world.EvaluatorFor(world.Member).GetEffectiveRoleAsync(world.DeepFile.ItemId));
        Assert.Equal(0, await world.EvaluatorFor(world.Stranger).GetEffectiveRoleAsync(world.DeepFile.ItemId));
    }

    [Fact]
    public async Task BrokenInheritance_BlocksAncestorGrants_ButOwnGrantsCount()
    {
        var world = await DriveTestWorld.CreateAsync();
        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.ManagerId);
        world.ChildFolder.BreakPermissionInheritance();
        await world.Db.SaveChangesAsync();
        var evaluator = world.EvaluatorFor(world.Member);

        Assert.Equal(DriveRole.ManagerId, await evaluator.GetEffectiveRoleAsync(world.RootFolder.ItemId));
        Assert.Equal(0, await evaluator.GetEffectiveRoleAsync(world.ChildFolder.ItemId));
        Assert.Equal(0, await evaluator.GetEffectiveRoleAsync(world.DeepFile.ItemId));

        await world.AddGrantAsync(world.ChildFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.ViewerId);
        Assert.Equal(DriveRole.ViewerId, await evaluator.GetEffectiveRoleAsync(world.DeepFile.ItemId));
    }

    [Fact]
    public async Task TrashedItem_ReachableByOwner_SpaceOwner_Head_AdminOnly()
    {
        var world = await DriveTestWorld.CreateAsync();
        world.MemberFile.Trash(world.Member.UserId);
        await world.Db.SaveChangesAsync();

        Assert.Equal(DriveRole.ManagerId, await world.EvaluatorFor(world.Member).GetEffectiveRoleAsync(world.MemberFile.ItemId));
        Assert.Equal(DriveRole.ManagerId, await world.EvaluatorFor(world.Owner).GetEffectiveRoleAsync(world.MemberFile.ItemId));
        Assert.Equal(DriveRole.ManagerId, await world.EvaluatorFor(world.Head).GetEffectiveRoleAsync(world.MemberFile.ItemId));
        Assert.Equal(DriveRole.ManagerId, await world.EvaluatorFor(world.Admin, isAdmin: true).GetEffectiveRoleAsync(world.MemberFile.ItemId));
        Assert.Equal(0, await world.EvaluatorFor(world.Stranger).GetEffectiveRoleAsync(world.MemberFile.ItemId));
    }

    [Fact]
    public async Task LiveItemUnderTrashedFolder_UnreachableEvenWithGrant()
    {
        var world = await DriveTestWorld.CreateAsync();
        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.ManagerId);
        world.RootFolder.Trash(world.Owner.UserId);
        await world.Db.SaveChangesAsync();

        Assert.Equal(0, await world.EvaluatorFor(world.Member).GetEffectiveRoleAsync(world.DeepFile.ItemId));
        // The space owner (like ADMIN) still manages their whole space.
        Assert.Equal(DriveRole.ManagerId, await world.EvaluatorFor(world.Owner).GetEffectiveRoleAsync(world.DeepFile.ItemId));
    }

    [Fact]
    public async Task Unauthenticated_OrUnknownItem_ReturnsZero()
    {
        var world = await DriveTestWorld.CreateAsync();
        var memberEvaluator = world.EvaluatorFor(world.Member);
        var evaluator = new DriveAccessEvaluator(
            new FakeUserContext(), world.Items, world.Spaces, world.Permissions, world.Db);

        Assert.Equal(0, await evaluator.GetEffectiveRoleAsync(world.DeepFile.ItemId));
        Assert.Equal(0, await memberEvaluator.GetEffectiveRoleAsync(Guid.NewGuid()));
        Assert.Equal(0, await memberEvaluator.GetEffectiveRoleAsync(Guid.Empty));
    }

    [Fact]
    public async Task CanAccessItem_ComparesAgainstRequiredRole()
    {
        var world = await DriveTestWorld.CreateAsync();
        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.ViewerId);
        var evaluator = world.EvaluatorFor(world.Member);

        Assert.True(await evaluator.CanAccessItemAsync(world.DeepFile.ItemId, DriveRole.ViewerId));
        Assert.False(await evaluator.CanAccessItemAsync(world.DeepFile.ItemId, DriveRole.EditorId));
        Assert.False(await evaluator.CanAccessItemAsync(world.DeepFile.ItemId, 99));
    }

    // ── Deletion approver ───────────────────────────────────────────────────

    [Fact]
    public async Task DeletionApprover_IsTheOwnersDepartmentHead()
    {
        var world = await DriveTestWorld.CreateAsync();
        var evaluator = world.EvaluatorFor(world.Admin, isAdmin: true);

        Assert.Equal(world.Head.UserId, await evaluator.GetDeletionApproverAsync(world.Member.UserId));
        Assert.True(await evaluator.IsDepartmentHeadOfAsync(world.Head.UserId, world.Member.UserId));
        Assert.False(await evaluator.IsDepartmentHeadOfAsync(world.Owner.UserId, world.Member.UserId));
        Assert.Null(await evaluator.GetDeletionApproverAsync(world.Stranger.UserId));
        Assert.Null(await evaluator.GetDeletionApproverAsync(Guid.Empty));
    }

    [Fact]
    public async Task DeletionApprover_NullWhenHeadInactive()
    {
        var world = await DriveTestWorld.CreateAsync();
        world.Head.Deactivate();
        await world.Db.SaveChangesAsync();
        var evaluator = world.EvaluatorFor(world.Admin, isAdmin: true);

        Assert.Null(await evaluator.GetDeletionApproverAsync(world.Member.UserId));
        Assert.False(await evaluator.IsDepartmentHeadOfAsync(world.Head.UserId, world.Member.UserId));
    }

    // ── Parity with the SQL browse predicate ────────────────────────────────

    [Fact]
    public async Task EvaluatorAndBrowsePredicate_AgreeForEveryCaller()
    {
        var world = await DriveTestWorld.CreateAsync();
        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.ViewerId);
        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.DepartmentId, world.Dept.DepartmentId, DriveRole.EditorId);
        world.ChildFolder.BreakPermissionInheritance();
        await world.AddGrantAsync(world.ChildFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.ManagerId);
        world.MemberFile.Trash(world.Member.UserId);
        await world.Db.SaveChangesAsync();

        var allItems = new[] { world.RootFolder, world.ChildFolder, world.DeepFile, world.MemberFile };
        var callers = new[]
        {
            (User: world.Owner, IsAdmin: false),
            (User: world.Member, IsAdmin: false),
            (User: world.Stranger, IsAdmin: false),
            (User: world.Head, IsAdmin: false),
            (User: world.Admin, IsAdmin: true)
        };

        foreach (var (user, isAdmin) in callers)
        {
            var evaluator = world.EvaluatorFor(user, isAdmin);
            var departments = DriveTestWorld.DepartmentIdsFor(user);

            var live = await world.Items.ListPagedAsync(AllRows(), new DriveItemFilter(), user.UserId, departments);
            var evaluatorLive = new List<Guid>();
            foreach (var item in allItems.Where(item => !item.IsTrashed))
                if (await evaluator.GetEffectiveRoleAsync(item.ItemId) > 0)
                    evaluatorLive.Add(item.ItemId);
            Assert.Equal(evaluatorLive.OrderBy(id => id), live.Items.Select(item => item.ItemId).OrderBy(id => id));
            Assert.Equal(evaluatorLive.Count, live.TotalCount);

            var trash = await world.Items.ListPagedAsync(AllRows(), new DriveItemFilter(Trashed: true), user.UserId, departments);
            var evaluatorTrash = new List<Guid>();
            foreach (var item in allItems.Where(item => item.IsTrashed))
                if (await evaluator.GetEffectiveRoleAsync(item.ItemId) > 0)
                    evaluatorTrash.Add(item.ItemId);
            Assert.Equal(evaluatorTrash.OrderBy(id => id), trash.Items.Select(item => item.ItemId).OrderBy(id => id));
            Assert.Equal(evaluatorTrash.Count, trash.TotalCount);
        }
    }
}