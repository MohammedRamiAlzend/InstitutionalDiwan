using System.Security.Claims;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Infrastructure.Authentication;
using InstitutionalDiwan.Server.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace InstitutionalDiwan.Tests.Authorization;

public sealed class PermissionAuthorizationHandlerTests
{
    private static ClaimsPrincipal PrincipalWithPermissions(params string[] permissions)
        => new(new ClaimsIdentity(
            permissions.Select(permission => new Claim(JwtProvider.PermissionClaimType, permission)),
            authenticationType: "Test"));

    private static async Task<bool> AuthorizeAsync(ClaimsPrincipal user, string requiredPermission)
    {
        var context = new AuthorizationHandlerContext(
            new[] { new PermissionRequirement(requiredPermission) },
            user,
            resource: null);

        await new PermissionAuthorizationHandler().HandleAsync(context);
        return context.HasSucceeded;
    }

    [Fact]
    public async Task Handle_WhenClaimMatchesRequiredPermission_Succeeds()
    {
        var principal = PrincipalWithPermissions(Permissions.UserManage, Permissions.DocumentView);

        Assert.True(await AuthorizeAsync(principal, Permissions.UserManage));
        Assert.True(await AuthorizeAsync(principal, Permissions.DocumentView));
    }

    [Fact]
    public async Task Handle_WhenPermissionClaimIsMissing_Fails()
    {
        var principal = PrincipalWithPermissions(Permissions.DocumentView);

        Assert.False(await AuthorizeAsync(principal, Permissions.UserManage));
    }

    [Fact]
    public async Task Handle_WhenPrincipalHasNoClaims_Fails()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        Assert.False(await AuthorizeAsync(principal, Permissions.DocumentView));
    }

    [Fact]
    public void PermissionCodes_AreUniqueAndComplete()
    {
        Assert.Equal(12, Permissions.All.Count);
        Assert.Equal(Permissions.All.Count, Permissions.All.Distinct().Count());
        Assert.Contains(Permissions.UserManage, Permissions.All);
        Assert.Contains(Permissions.DocumentApprove, Permissions.All);
        Assert.Contains(Permissions.NumberingView, Permissions.All);
    }
}
