using InstitutionalDiwan.Domain.Entities.Identity;

namespace InstitutionalDiwan.Application.Abstractions.Authentication;

/// <summary>A signed access token and the UTC instant at which it expires.</summary>
public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);

/// <summary>Creates signed JWT access tokens for an authenticated <see cref="User"/>.</summary>
public interface IJwtProvider
{
    /// <summary>
    /// Generates an access token carrying the user's identity, department, scope, role and permission
    /// claims.
    /// </summary>
    AccessToken GenerateAccessToken(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions);
}
