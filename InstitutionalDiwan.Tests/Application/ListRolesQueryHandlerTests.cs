using InstitutionalDiwan.Application.Features.Identity.Roles;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListRolesQueryHandlerTests
{
    private static Role CreateRole(string code, params string[] permissionCodes)
    {
        var role = Role.Create(code, "مدير");
        Assert.True(role.IsSuccess);
        foreach (var permissionCode in permissionCodes)
            role.Value.GrantPermission(permissionCode);
        return role.Value;
    }

    [Fact]
    public async Task Handle_WithNoRoles_ReturnsEmptyCollection()
    {
        var result = await new ListRolesQueryHandler(new FakeRoleRepository())
            .Handle(new ListRolesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsRolesOrderedByCodeWithPermissions()
    {
        var roles = new FakeRoleRepository();
        roles.Add(CreateRole("USER", "DOCUMENT_VIEW"));
        roles.Add(CreateRole("ADMIN", "ROLE_MANAGE", "USER_MANAGE"));

        var result = await new ListRolesQueryHandler(roles)
            .Handle(new ListRolesQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Equal(new[] { "ADMIN", "USER" }, result.Value.Select(role => role.Code));

        var admin = result.Value.First(role => role.Code == "ADMIN");
        Assert.Equal(new[] { "ROLE_MANAGE", "USER_MANAGE" }, admin.Permissions);
    }
}

public sealed class GetRoleByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenRoleExists_ReturnsMappedResponse()
    {
        var roles = new FakeRoleRepository();
        var role = Role.Create("MANAGER", "مدير", "Manager");
        Assert.True(role.IsSuccess);
        role.Value.GrantPermission("USER_MANAGE");
        roles.Add(role.Value);

        var result = await new GetRoleByIdQueryHandler(roles)
            .Handle(new GetRoleByIdQuery(role.Value.RoleId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(role.Value.RoleId, result.Value.RoleId);
        Assert.Equal("MANAGER", result.Value.Code);
        Assert.Equal("مدير", result.Value.NameAr);
        Assert.Equal("Manager", result.Value.NameEn);
        Assert.Equal(new[] { "USER_MANAGE" }, result.Value.Permissions);
    }

    [Fact]
    public async Task Handle_WhenRoleIsMissing_ReturnsNotFound()
    {
        var result = await new GetRoleByIdQueryHandler(new FakeRoleRepository())
            .Handle(new GetRoleByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(DomainErrors.Identity.RoleNotFound.Code, result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }
}
