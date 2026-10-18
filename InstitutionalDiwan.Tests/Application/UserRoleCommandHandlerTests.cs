using InstitutionalDiwan.Application.Features.Identity.Users;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class UserRoleCommandHandlerTests
{
    private static (FakeUserRepository Users, FakeRoleRepository Roles, User User, Role Role) Setup()
    {
        var role = Role.Create("MANAGER", "مدير", "Manager");
        Assert.True(role.IsSuccess);

        var user = User.Create("manager", "hash", Guid.NewGuid());
        Assert.True(user.IsSuccess);

        var users = new FakeUserRepository();
        users.Add(user.Value);
        var roles = new FakeRoleRepository();
        roles.Add(role.Value);

        return (users, roles, user.Value, role.Value);
    }

    [Fact]
    public async Task AssignRole_WithValidRole_AssignsAndSaves()
    {
        var (users, roles, user, role) = Setup();
        var unitOfWork = new FakeUnitOfWork();

        var result = await new AssignUserRoleCommandHandler(users, roles, unitOfWork).Handle(
            new AssignUserRoleCommand(user.UserId, role.RoleId, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "MANAGER" }, result.Value.Roles);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Assign_WhenRoleMissing_ReturnsNotFound()
    {
        var (users, _, user, _) = Setup();

        var result = await new AssignUserRoleCommandHandler(
            users, new FakeRoleRepository(), new FakeUnitOfWork()).Handle(
            new AssignUserRoleCommand(user.UserId, Guid.NewGuid()),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Fact]
    public async Task Assign_WhenAlreadyAssigned_ReturnsConflict()
    {
        var (users, roles, user, _) = Setup();
        var role = roles.Roles[0];
        Assert.True(user.AssignRole(role.RoleId).IsSuccess);

        var result = await new AssignUserRoleCommandHandler(users, roles, new FakeUnitOfWork()).Handle(
            new AssignUserRoleCommand(user.UserId, role.RoleId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleAlreadyAssigned.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task Revoke_WithAssignedRole_RemovesRoleAndSaves()
    {
        var (users, roles, user, _) = Setup();
        var role = roles.Roles[0];
        Assert.True(user.AssignRole(role.RoleId).IsSuccess);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new RevokeUserRoleCommandHandler(users, roles, unitOfWork).Handle(
            new RevokeUserRoleCommand(user.UserId, role.RoleId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Roles);
        Assert.Empty(user.UserRoles);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Revoke_WhenNotAssigned_ReturnsNotFound()
    {
        var (users, roles, user, _) = Setup();

        var result = await new RevokeUserRoleCommandHandler(users, roles, new FakeUnitOfWork()).Handle(
            new RevokeUserRoleCommand(user.UserId, roles.Roles[0].RoleId),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleNotAssigned.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
