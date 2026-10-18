using InstitutionalDiwan.Application.Features.Identity.Roles;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class RevokeRolePermissionCommandHandlerTests
{
    private static Role AddRoleWithPermission(FakeRoleRepository roles, string permissionCode)
    {
        var role = Role.Create("MANAGER", "مدير");
        Assert.True(role.IsSuccess);
        role.Value.GrantPermission(permissionCode);
        roles.Add(role.Value);
        return role.Value;
    }

    [Fact]
    public async Task Handle_WhenPermissionIsGranted_RevokesAndSaves()
    {
        var roles = new FakeRoleRepository();
        var role = AddRoleWithPermission(roles, "ROLE_MANAGE");
        var unitOfWork = new FakeUnitOfWork();

        var result = await new RevokeRolePermissionCommandHandler(roles, unitOfWork).Handle(
            new RevokeRolePermissionCommand(role.RoleId, "ROLE_MANAGE"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain("ROLE_MANAGE", result.Value.Permissions);
        Assert.Empty(role.RolePermissions);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenPermissionIsNotGranted_IsNoOpAndSaves()
    {
        var roles = new FakeRoleRepository();
        var role = AddRoleWithPermission(roles, "ROLE_MANAGE");
        var unitOfWork = new FakeUnitOfWork();

        var result = await new RevokeRolePermissionCommandHandler(roles, unitOfWork).Handle(
            new RevokeRolePermissionCommand(role.RoleId, "USER_MANAGE"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("ROLE_MANAGE", result.Value.Permissions);
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenRoleIsMissing_ReturnsNotFound()
    {
        var result = await new RevokeRolePermissionCommandHandler(new FakeRoleRepository(), new FakeUnitOfWork())
            .Handle(new RevokeRolePermissionCommand(Guid.NewGuid(), "ROLE_MANAGE"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
