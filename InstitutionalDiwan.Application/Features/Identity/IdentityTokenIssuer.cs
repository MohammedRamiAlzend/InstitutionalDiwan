using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Features.Identity.Login;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity;

/// <summary>
/// Builds the authentication response (signed JWT plus identity context) for a user.
/// Shared by Login and ChangePassword so both issue identical tokens.
/// </summary>
internal static class IdentityTokenIssuer
{
    public static async Task<LoginResponse> IssueAsync(
        User user,
        IRoleRepository roleRepository,
        IJwtProvider jwtProvider,
        CancellationToken cancellationToken)
    {
        var roles = new List<Role>();
        foreach (var userRole in user.UserRoles)
        {
            var role = await roleRepository.GetByIdAsync(userRole.RoleId, cancellationToken);
            if (role is { IsActive: true })
                roles.Add(role);
        }

        var roleCodes = roles.Select(role => role.Code).Distinct().ToList();
        var permissions = roles
            .SelectMany(role => role.RolePermissions)
            .Select(permission => permission.PermissionCode)
            .Distinct()
            .ToList();

        var accessToken = jwtProvider.GenerateAccessToken(user, roleCodes, permissions);

        return new LoginResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            user.UserId,
            user.Username,
            user.MustChangePassword,
            roleCodes,
            permissions);
    }
}
