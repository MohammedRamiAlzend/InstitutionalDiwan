using InstitutionalDiwan.Application.Features.Identity.Users;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListUsersQueryHandlerTests
{
    private static (Role AdminRole, Role UserRole) CreateRoles()
    {
        var adminRole = Role.Create("ADMIN", "مسؤول إداري", "Administrator", isSystemRole: true);
        var userRole = Role.Create("USER", "مستخدم", "User");
        Assert.True(adminRole.IsSuccess);
        Assert.True(userRole.IsSuccess);
        return (adminRole.Value, userRole.Value);
    }

    [Fact]
    public async Task Handle_ReturnsUsersWithTheirRoles()
    {
        var (adminRole, userRole) = CreateRoles();
        var roles = new FakeRoleRepository();
        roles.Add(adminRole);
        roles.Add(userRole);

        var users = new FakeUserRepository();
        var admin = User.Create("admin", "hash-a", Guid.Empty, mustChangePassword: false);
        var standard = User.Create("user", "hash-u", Guid.Empty, mustChangePassword: false);
        Assert.True(admin.IsSuccess);
        Assert.True(standard.IsSuccess);
        Assert.True(admin.Value.AssignRole(adminRole.RoleId).IsSuccess);
        Assert.True(standard.Value.AssignRole(userRole.RoleId).IsSuccess);
        users.Add(admin.Value);
        users.Add(standard.Value);

        var handler = new ListUsersQueryHandler(users, roles);
        var result = await handler.Handle(new ListUsersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Items.Count);
        Assert.Equal(2, result.Value.TotalCount);

        var adminSummary = result.Value.Items.Single(user => user.Username == "admin");
        Assert.Equal(new[] { "ADMIN" }, adminSummary.Roles);
        Assert.True(adminSummary.IsActive);
        Assert.False(adminSummary.MustChangePassword);

        var userSummary = result.Value.Items.Single(user => user.Username == "user");
        Assert.Equal(new[] { "USER" }, userSummary.Roles);
    }

    [Fact]
    public async Task Handle_WithNoUsers_ReturnsEmptyPage()
    {
        var handler = new ListUsersQueryHandler(new FakeUserRepository(), new FakeRoleRepository());

        var result = await handler.Handle(new ListUsersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalCount);
    }

    [Fact]
    public async Task Handle_PagesUsersAndReportsTheTotal()
    {
        var roles = new FakeRoleRepository();
        var users = new FakeUserRepository();
        for (var i = 0; i < 25; i++)
            users.Add(User.Create($"user-{i:00}", "hash", Guid.Empty, mustChangePassword: false).Value);

        var result = await new ListUsersQueryHandler(users, roles).Handle(
            new ListUsersQuery(Page: 2, PageSize: 10), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10, result.Value.Items.Count);
        Assert.Equal(25, result.Value.TotalCount);
        Assert.Equal(3, result.Value.TotalPages);
        Assert.True(result.Value.HasNext);
    }

    [Fact]
    public async Task Handle_SearchesByUsername()
    {
        var roles = new FakeRoleRepository();
        var users = new FakeUserRepository();
        users.Add(User.Create("admin", "hash", Guid.Empty, mustChangePassword: false).Value);
        users.Add(User.Create("editor", "hash", Guid.Empty, mustChangePassword: false).Value);

        var result = await new ListUsersQueryHandler(users, roles).Handle(
            new ListUsersQuery(Search: "adm"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var user = Assert.Single(result.Value.Items);
        Assert.Equal("admin", user.Username);
    }

    [Fact]
    public async Task Handle_SkipsRolesThatNoLongerExist()
    {
        var users = new FakeUserRepository();
        var roles = new FakeRoleRepository(); // role repository is empty

        var user = User.Create("orphan", "hash", Guid.Empty, mustChangePassword: false);
        Assert.True(user.IsSuccess);
        Assert.True(user.Value.AssignRole(Guid.NewGuid()).IsSuccess);
        users.Add(user.Value);

        var handler = new ListUsersQueryHandler(users, roles);
        var result = await handler.Handle(new ListUsersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(Assert.Single(result.Value.Items).Roles);
    }
}
