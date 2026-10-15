using InstitutionalDiwan.Application.Features.Identity.ListUsers;
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
        Assert.Equal(2, result.Value.Count);

        var adminSummary = result.Value.Single(user => user.Username == "admin");
        Assert.Equal(new[] { "ADMIN" }, adminSummary.Roles);
        Assert.True(adminSummary.IsActive);
        Assert.False(adminSummary.MustChangePassword);

        var userSummary = result.Value.Single(user => user.Username == "user");
        Assert.Equal(new[] { "USER" }, userSummary.Roles);
    }

    [Fact]
    public async Task Handle_WithNoUsers_ReturnsEmptyCollection()
    {
        var handler = new ListUsersQueryHandler(new FakeUserRepository(), new FakeRoleRepository());

        var result = await handler.Handle(new ListUsersQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
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
        Assert.Empty(Assert.Single(result.Value).Roles);
    }
}
