using InstitutionalDiwan.Application.Features.Identity.Roles;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class GrantRolePermissionCommandHandlerTests
{
    private static FakePermissionRepository Catalog()
    {
        var permissions = new FakePermissionRepository();
        var permission = Permission.Create("ROLE_MANAGE", "إدارة الأدوار", "Identity");
        Assert.True(permission.IsSuccess);
        permissions.Permissions.Add(permission.Value);
        return permissions;
    }

    private static Role AddRole(FakeRoleRepository roles)
    {
        var role = Role.Create("MANAGER", "مدير");
        Assert.True(role.IsSuccess);
        roles.Add(role.Value);
        return role.Value;
    }

    [Fact]
    public async Task Handle_WithKnownPermission_GrantsAndSaves()
    {
        var roles = new FakeRoleRepository();
        var role = AddRole(roles);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new GrantRolePermissionCommandHandler(roles, Catalog(), unitOfWork).Handle(
            new GrantRolePermissionCommand(role.RoleId, "role_manage"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Contains("ROLE_MANAGE", result.Value.Permissions);
        Assert.Contains(role.RolePermissions, permission => permission.PermissionCode == "ROLE_MANAGE");
        Assert.Equal(1, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenPermissionIsUnknown_ReturnsValidationError()
    {
        var roles = new FakeRoleRepository();
        var role = AddRole(roles);
        var unitOfWork = new FakeUnitOfWork();

        var result = await new GrantRolePermissionCommandHandler(roles, Catalog(), unitOfWork).Handle(
            new GrantRolePermissionCommand(role.RoleId, "NOT_A_PERMISSION"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.PermissionNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenPermissionAlreadyGranted_ReturnsConflict()
    {
        var roles = new FakeRoleRepository();
        var role = AddRole(roles);
        role.GrantPermission("ROLE_MANAGE");
        var unitOfWork = new FakeUnitOfWork();

        var result = await new GrantRolePermissionCommandHandler(roles, Catalog(), unitOfWork).Handle(
            new GrantRolePermissionCommand(role.RoleId, "ROLE_MANAGE"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.PermissionAlreadyGranted.Code, result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
        Assert.Equal(0, unitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task Handle_WhenRoleIsMissing_ReturnsNotFound()
    {
        var result = await new GrantRolePermissionCommandHandler(
                new FakeRoleRepository(), Catalog(), new FakeUnitOfWork())
            .Handle(new GrantRolePermissionCommand(Guid.NewGuid(), "ROLE_MANAGE"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
