using InstitutionalDiwan.Application.Features.Identity.Permissions;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class ListPermissionsQueryHandlerTests
{
    private static Permission CreatePermission(string code, string moduleName)
    {
        var permission = Permission.Create(code, "صلاحية", moduleName);
        Assert.True(permission.IsSuccess);
        return permission.Value;
    }

    [Fact]
    public async Task Handle_WithNoPermissions_ReturnsEmptyCollection()
    {
        var result = await new ListPermissionsQueryHandler(new FakePermissionRepository())
            .Handle(new ListPermissionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ReturnsPermissionsOrderedByModuleThenCode()
    {
        var permissions = new FakePermissionRepository();
        permissions.Permissions.Add(CreatePermission("USER_MANAGE", "IDENTITY"));
        permissions.Permissions.Add(CreatePermission("DOCUMENT_VIEW", "CORRESPONDENCE"));
        permissions.Permissions.Add(CreatePermission("ROLE_MANAGE", "IDENTITY"));

        var result = await new ListPermissionsQueryHandler(permissions)
            .Handle(new ListPermissionsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(
            new[] { "DOCUMENT_VIEW", "ROLE_MANAGE", "USER_MANAGE" },
            result.Value.Select(permission => permission.Code));
        Assert.Equal("IDENTITY", result.Value.First(permission => permission.Code == "ROLE_MANAGE").ModuleName);
    }
}
