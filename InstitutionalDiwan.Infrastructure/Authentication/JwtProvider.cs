using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace InstitutionalDiwan.Infrastructure.Authentication;

public sealed class JwtProvider : IJwtProvider
{
    /// <summary>Custom claim type carrying a single granted permission code.</summary>
    public const string PermissionClaimType = "permission";

    private readonly JwtOptions _options;

    private readonly IClock _clock;

    public JwtProvider(IOptions<JwtOptions> options, IClock clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public AccessToken GenerateAccessToken(
        User user,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> permissions)
    {
        var now = _clock.UtcNow;
        var expiresAt = now.AddMinutes(_options.AccessTokenExpirationMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("dept_id", user.PrimaryDepartmentId.ToString()),
            new("scope_type", user.ScopeTypeId.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new("must_change_password", user.MustChangePassword ? "true" : "false"),
            new(JwtSessionStamp.ClaimType, JwtSessionStamp.Create(user, roles, permissions, _options.SecretKey))
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(permission => new Claim(PermissionClaimType, permission)));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
