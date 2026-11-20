using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Persistence;

/// <summary>
/// Drive repositories against a real SQLite database: tree walks, browse filters (including the
/// SQL access predicate), job expiry lists and the uniqueness constraints from the EF model.
/// </summary>
public sealed class DriveRepositoryTests
{
    private static readonly DateTime Now = TestClock.Instance.UtcNow;

    private static PageRequest AllRows(string? sortBy = null, bool descending = false) =>
        new(1, 100, null, sortBy, descending);

    // ── Tree walks ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Ancestors_ReturnParentToRootOrder_AndDepth()
    {
        var world = await DriveTestWorld.CreateAsync();

        var ancestors = await world.Items.GetAncestorIdsAsync(world.DeepFile.ItemId);

        Assert.Equal(new[] { world.ChildFolder.ItemId, world.RootFolder.ItemId }, ancestors);
        Assert.Equal(3, await world.Items.GetDepthAsync(world.DeepFile.ItemId));
        Assert.Empty(await world.Items.GetAncestorIdsAsync(world.RootFolder.ItemId));
        Assert.Equal(1, await world.Items.GetDepthAsync(world.RootFolder.ItemId));
    }

    [Fact]
    public async Task NameExists_IgnoresTrashedSiblings_AndExcludesSelf()
    {
        var world = await DriveTestWorld.CreateAsync();
        var folder = await world.AddFolderAsync("عقود");

        Assert.True(await world.Items.NameExistsAsync(world.Shared.SpaceId, null, "عقود"));
        Assert.False(await world.Items.NameExistsAsync(world.Shared.SpaceId, null, "عقود", folder.ItemId));
        Assert.False(await world.Items.NameExistsAsync(world.Shared.SpaceId, world.RootFolder.ItemId, "عقود"));

        folder.Trash(world.Owner.UserId);
        await world.Db.SaveChangesAsync();

        Assert.False(await world.Items.NameExistsAsync(world.Shared.SpaceId, null, "عقود"));
    }

    [Fact]
    public async Task Children_ExcludeTrashed_AndOrderFoldersFirst()
    {
        var world = await DriveTestWorld.CreateAsync();
        await world.AddFolderAsync("أرشيف");
        world.RootFolder.Trash(world.Owner.UserId);
        await world.Db.SaveChangesAsync();

        var children = await world.Items.GetChildrenAsync(world.Shared.SpaceId, null);

        Assert.Equal(2, children.Count);
        Assert.All(children.Take(1), item => Assert.True(item.IsFolder));
        Assert.DoesNotContain(children, item => item.ItemId == world.RootFolder.ItemId);
    }

    // ── Versions ────────────────────────────────────────────────────────────

    [Fact]
    public async Task NextVersionNumber_StartsAtOne_AndFollowsMax()
    {
        var world = await DriveTestWorld.CreateAsync();

        Assert.Equal(1, await world.Versions.GetNextVersionNumberAsync(world.DeepFile.ItemId));

        await world.AddVersionAsync(world.DeepFile, 1, commit: true);
        await world.AddVersionAsync(world.DeepFile, 2);

        Assert.Equal(3, await world.Versions.GetNextVersionNumberAsync(world.DeepFile.ItemId));
        Assert.Equal(2, (await world.Versions.ListByItemAsync(world.DeepFile.ItemId)).Count);
    }

    [Fact]
    public async Task PendingOlderThan_ReturnsOnlyStalePending()
    {
        var world = await DriveTestWorld.CreateAsync();
        var stale = await world.AddVersionAsync(world.DeepFile, 1, Now.AddHours(-2));
        await world.AddVersionAsync(world.DeepFile, 2, Now.AddHours(-2), commit: true);
        await world.AddVersionAsync(world.DeepFile, 3, Now);

        var pending = await world.Versions.ListPendingOlderThanAsync(Now.AddHours(-1), 10);

        Assert.Equal(new[] { stale.VersionId }, pending.Select(version => version.VersionId));
    }

    // ── Browse: access ──────────────────────────────────────────────────────

    [Fact]
    public async Task List_SpaceOwner_SeesWholeSpace_FoldersFirst()
    {
        var world = await DriveTestWorld.CreateAsync();

        var page = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(SpaceId: world.Shared.SpaceId),
            world.Owner.UserId, DriveTestWorld.DepartmentIdsFor(world.Owner));

        Assert.Equal(4, page.TotalCount);
        Assert.True(page.Items[0].IsFolder);
        Assert.True(page.Items[1].IsFolder);
        Assert.True(page.Items[2].IsFile);
        Assert.True(page.Items[3].IsFile);
    }

    [Fact]
    public async Task List_Stranger_SeesNothing()
    {
        var world = await DriveTestWorld.CreateAsync();

        var page = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(),
            world.Stranger.UserId, DriveTestWorld.DepartmentIdsFor(world.Stranger));

        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }

    [Fact]
    public async Task List_UserGrantOnFolder_ReachesDescendants()
    {
        var world = await DriveTestWorld.CreateAsync();
        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.ViewerId);

        var page = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(),
            world.Member.UserId, DriveTestWorld.DepartmentIdsFor(world.Member));

        Assert.Equal(3, page.TotalCount);
        Assert.DoesNotContain(page.Items, item => item.ItemId == world.MemberFile.ItemId);
    }

    [Fact]
    public async Task List_DepartmentGrant_ReachesDepartmentMembersOnly()
    {
        var world = await DriveTestWorld.CreateAsync();
        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.DepartmentId, world.Dept.DepartmentId, DriveRole.EditorId);

        var memberPage = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(),
            world.Member.UserId, DriveTestWorld.DepartmentIdsFor(world.Member));
        var strangerPage = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(),
            world.Stranger.UserId, DriveTestWorld.DepartmentIdsFor(world.Stranger));

        Assert.Equal(3, memberPage.TotalCount);
        Assert.Equal(0, strangerPage.TotalCount);
    }

    [Fact]
    public async Task List_BrokenInheritance_BlocksAncestorGrants()
    {
        var world = await DriveTestWorld.CreateAsync();
        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.ViewerId);
        world.ChildFolder.BreakPermissionInheritance();
        await world.Db.SaveChangesAsync();

        var page = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(),
            world.Member.UserId, DriveTestWorld.DepartmentIdsFor(world.Member));

        Assert.Equal(new[] { world.RootFolder.ItemId }, page.Items.Select(item => item.ItemId));
    }

    [Fact]
    public async Task List_BrokenNode_OwnGrantsStillCount()
    {
        var world = await DriveTestWorld.CreateAsync();
        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.ViewerId);
        world.ChildFolder.BreakPermissionInheritance();
        await world.AddGrantAsync(world.ChildFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.EditorId);

        var page = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(),
            world.Member.UserId, DriveTestWorld.DepartmentIdsFor(world.Member));

        Assert.Equal(3, page.TotalCount);
        Assert.Contains(page.Items, item => item.ItemId == world.DeepFile.ItemId);
    }

    [Fact]
    public async Task List_TrashedFolder_HidesItsWholeSubtree()
    {
        var world = await DriveTestWorld.CreateAsync();
        world.RootFolder.Trash(world.Owner.UserId);
        await world.Db.SaveChangesAsync();

        var live = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(),
            world.Owner.UserId, DriveTestWorld.DepartmentIdsFor(world.Owner));
        var trash = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(Trashed: true),
            world.Owner.UserId, DriveTestWorld.DepartmentIdsFor(world.Owner));

        Assert.Equal(new[] { world.MemberFile.ItemId }, live.Items.Select(item => item.ItemId));
        Assert.Equal(new[] { world.RootFolder.ItemId }, trash.Items.Select(item => item.ItemId));
    }

    [Fact]
    public async Task TrashView_VisibleToOwner_SpaceOwner_Head_AndAdminOnly()
    {
        var world = await DriveTestWorld.CreateAsync();
        world.MemberFile.Trash(world.Member.UserId);
        await world.Db.SaveChangesAsync();
        var filter = new DriveItemFilter(Trashed: true);

        foreach (var caller in new[] { world.Member, world.Owner, world.Head, world.Admin })
        {
            var page = await world.Items.ListPagedAsync(
                AllRows(), filter, caller.UserId, DriveTestWorld.DepartmentIdsFor(caller));
            Assert.Equal(new[] { world.MemberFile.ItemId }, page.Items.Select(item => item.ItemId));
        }

        var strangerPage = await world.Items.ListPagedAsync(
            AllRows(), filter, world.Stranger.UserId, DriveTestWorld.DepartmentIdsFor(world.Stranger));
        Assert.Empty(strangerPage.Items);
    }

    // ── Browse: filters, sort, paging ───────────────────────────────────────

    [Fact]
    public async Task List_StarredOnly_Search_Kind_AndModifiedFilters()
    {
        var world = await DriveTestWorld.CreateAsync();
        world.Db.DriveStars.Add(DriveStar.Create(world.Owner.UserId, world.DeepFile.ItemId).Value);
        await world.Db.SaveChangesAsync();
        var caller = world.Owner.UserId;
        var departments = DriveTestWorld.DepartmentIdsFor(world.Owner);

        var starred = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(StarredOnly: true), caller, departments);
        Assert.Equal(new[] { world.DeepFile.ItemId }, starred.Items.Select(item => item.ItemId));

        var searched = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(Search: "تقرير"), caller, departments);
        Assert.Equal(new[] { world.DeepFile.ItemId }, searched.Items.Select(item => item.ItemId));

        var files = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(ItemKindId: DriveItemKind.FileId), caller, departments);
        Assert.Equal(2, files.TotalCount);
        Assert.All(files.Items, item => Assert.True(item.IsFile));

        var future = await world.Items.ListPagedAsync(
            AllRows(), new DriveItemFilter(ModifiedFrom: DateTime.UtcNow.AddHours(1)), caller, departments);
        Assert.Equal(0, future.TotalCount);
    }

    [Fact]
    public async Task List_Paging_ReturnsSliceWithFullCount()
    {
        var world = await DriveTestWorld.CreateAsync();

        var first = await world.Items.ListPagedAsync(
            new PageRequest(1, 2), new DriveItemFilter(),
            world.Owner.UserId, DriveTestWorld.DepartmentIdsFor(world.Owner));
        var second = await world.Items.ListPagedAsync(
            new PageRequest(2, 2), new DriveItemFilter(),
            world.Owner.UserId, DriveTestWorld.DepartmentIdsFor(world.Owner));

        Assert.Equal(4, first.TotalCount);
        Assert.Equal(2, first.Items.Count);
        Assert.Equal(4, second.TotalCount);
        Assert.Equal(2, second.Items.Count);
        Assert.Empty(first.Items.Select(item => item.ItemId).Intersect(second.Items.Select(item => item.ItemId)));
    }

    [Fact]
    public async Task List_NameSortDescending_ReversesOrder()
    {
        var world = await DriveTestWorld.CreateAsync();

        var ascending = await world.Items.ListPagedAsync(
            AllRows("name"), new DriveItemFilter(),
            world.Owner.UserId, DriveTestWorld.DepartmentIdsFor(world.Owner));
        var descending = await world.Items.ListPagedAsync(
            AllRows("name", true), new DriveItemFilter(),
            world.Owner.UserId, DriveTestWorld.DepartmentIdsFor(world.Owner));

        Assert.Equal(
            ascending.Items.Select(item => item.ItemId).Reverse(),
            descending.Items.Select(item => item.ItemId));
    }

    // ── Spaces ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SharedSpaces_ListedForOwnerOrGrantHolderOnly()
    {
        var world = await DriveTestWorld.CreateAsync();

        var ownerSpaces = await world.Spaces.ListSharedForUserAsync(world.Owner.UserId, DriveTestWorld.DepartmentIdsFor(world.Owner));
        Assert.Equal(new[] { world.Shared.SpaceId }, ownerSpaces.Select(space => space.SpaceId));

        var memberBefore = await world.Spaces.ListSharedForUserAsync(world.Member.UserId, DriveTestWorld.DepartmentIdsFor(world.Member));
        Assert.Empty(memberBefore);

        await world.AddGrantAsync(world.RootFolder.ItemId, DrivePrincipalType.UserId, world.Member.UserId, DriveRole.ViewerId);
        var memberAfter = await world.Spaces.ListSharedForUserAsync(world.Member.UserId, DriveTestWorld.DepartmentIdsFor(world.Member));
        Assert.Equal(new[] { world.Shared.SpaceId }, memberAfter.Select(space => space.SpaceId));

        var strangerSpaces = await world.Spaces.ListSharedForUserAsync(world.Stranger.UserId, DriveTestWorld.DepartmentIdsFor(world.Stranger));
        Assert.Equal(new[] { world.Foreign.SpaceId }, strangerSpaces.Select(space => space.SpaceId));
    }

    [Fact]
    public async Task PersonalSpace_FoundByOwner()
    {
        var world = await DriveTestWorld.CreateAsync();

        var personal = await world.Spaces.GetPersonalByOwnerAsync(world.Owner.UserId);

        Assert.NotNull(personal);
        Assert.Equal(world.Personal.SpaceId, personal!.SpaceId);
        Assert.Null(await world.Spaces.GetPersonalByOwnerAsync(world.Member.UserId));
    }

    // ── Job expiry lists ────────────────────────────────────────────────────

    [Fact]
    public async Task TrashExpired_ReturnsOnlyOldRoots_OldestFirst()
    {
        var world = await DriveTestWorld.CreateAsync();
        world.RootFolder.Trash(world.Owner.UserId, Now.AddDays(-31));
        world.MemberFile.Trash(world.Member.UserId, Now.AddDays(-1));
        await world.Db.SaveChangesAsync();

        var expired = await world.Items.ListTrashExpiredAsync(Now.AddDays(-30), 10);

        Assert.Equal(new[] { world.RootFolder.ItemId }, expired.Select(item => item.ItemId));
    }

    [Fact]
    public async Task IdleOpenSessions_ReturnsOnlyStaleOpenOnes()
    {
        var world = await DriveTestWorld.CreateAsync();
        var version = await world.AddVersionAsync(world.DeepFile, 1, commit: true);
        var stale = DriveEditSession.Open(world.DeepFile.ItemId, world.Owner.UserId, "key-stale", version.VersionId, Now.AddHours(-3)).Value;
        var fresh = DriveEditSession.Open(world.MemberFile.ItemId, world.Member.UserId, "key-fresh", version.VersionId, Now).Value;
        var closed = DriveEditSession.Open(world.DeepFile.ItemId, world.Member.UserId, "key-closed", version.VersionId, Now.AddHours(-3)).Value;
        closed.Close();
        world.Db.DriveEditSessions.AddRange(stale, fresh, closed);
        await world.Db.SaveChangesAsync();

        var idle = await world.Sessions.ListIdleOpenAsync(Now.AddHours(-2), 10);

        Assert.Equal(new[] { stale.SessionId }, idle.Select(session => session.SessionId));
        Assert.NotNull(await world.Sessions.FindOpenByKeyAsync("key-fresh"));
        Assert.Null(await world.Sessions.FindOpenByKeyAsync("key-closed"));
    }

    // ── Uniqueness constraints ──────────────────────────────────────────────

    [Fact]
    public async Task PersonalSpace_UniquePerOwner()
    {
        var world = await DriveTestWorld.CreateAsync();
        world.Db.DriveSpaces.Add(DriveSpace.CreatePersonal(world.Owner.UserId).Value);

        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task LiveSiblingName_Unique_TrashedSiblingFreed()
    {
        var world = await DriveTestWorld.CreateAsync();
        // Under a folder (non-null ParentId): SQLite unique indexes treat NULL parents as distinct,
        // so root-level name uniqueness is enforced by NameExistsAsync in the application layer.
        var parentId = world.RootFolder.ItemId;
        var folder = await world.AddFolderAsync("عقود", parentId);
        folder.Trash(world.Owner.UserId);
        await world.Db.SaveChangesAsync();

        // A trashed sibling frees the name...
        await world.AddFolderAsync("عقود", parentId);

        // ...but a second live sibling with the same name violates the filtered index.
        world.Db.DriveItems.Add(DriveItem.CreateFolder(world.Shared.SpaceId, parentId, "عقود", world.Owner.UserId, depth: 2).Value);
        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task PendingDeletionRequest_UniquePerItem()
    {
        var world = await DriveTestWorld.CreateAsync();
        world.Db.DriveDeletionRequests.Add(
            DriveDeletionRequest.Create(world.MemberFile.ItemId, world.Member.UserId, "سبب").Value);
        await world.Db.SaveChangesAsync();

        world.Db.DriveDeletionRequests.Add(
            DriveDeletionRequest.Create(world.MemberFile.ItemId, world.Member.UserId, "سبب آخر").Value);
        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task OpenEditSessionKey_Unique()
    {
        var world = await DriveTestWorld.CreateAsync();
        var version = await world.AddVersionAsync(world.DeepFile, 1, commit: true);
        world.Db.DriveEditSessions.Add(
            DriveEditSession.Open(world.DeepFile.ItemId, world.Owner.UserId, "key-1", version.VersionId).Value);
        await world.Db.SaveChangesAsync();

        world.Db.DriveEditSessions.Add(
            DriveEditSession.Open(world.DeepFile.ItemId, world.Member.UserId, "key-1", version.VersionId).Value);
        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }

    [Fact]
    public async Task Star_UniquePerUserAndItem()
    {
        var world = await DriveTestWorld.CreateAsync();
        world.Db.DriveStars.Add(DriveStar.Create(world.Owner.UserId, world.DeepFile.ItemId).Value);
        await world.Db.SaveChangesAsync();

        world.Db.DriveStars.Add(DriveStar.Create(world.Owner.UserId, world.DeepFile.ItemId).Value);
        await Assert.ThrowsAsync<DbUpdateException>(() => world.Db.SaveChangesAsync());
    }
}