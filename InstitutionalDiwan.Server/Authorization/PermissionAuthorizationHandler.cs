using InstitutionalDiwan.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace InstitutionalDiwan.Server.Authorization;

/// <summary>
/// Succeeds when the principal has a <c>permission</c> claim whose value equals the required
/// permission code (the claim type is defined by <see cref="JwtProvider.PermissionClaimType"/>).
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        if (context.User.HasClaim(JwtProvider.PermissionClaimType, requirement.Permission))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
