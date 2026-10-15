using System.Text;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Infrastructure.Authentication;
using InstitutionalDiwan.Server.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace InstitutionalDiwan.Server.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>Configures JWT bearer authentication using the <c>Jwt</c> configuration section.</summary>
    public static IServiceCollection AddInstitutionalAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();

        // Fail fast when no signing key is configured (user-secrets in Development, the
        // Jwt__SecretKey environment variable in production) instead of failing later while
        // issuing or validating the first token.
        if (string.IsNullOrWhiteSpace(jwt.SecretKey) || Encoding.UTF8.GetByteCount(jwt.SecretKey) < 32)
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey is missing or shorter than 32 bytes. Set it with " +
                "'dotnet user-secrets set \"Jwt:SecretKey\" <value>' in Development or via the " +
                "Jwt__SecretKey environment variable in production.");
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        // One policy per permission code: the policy name must equal the "permission" claim value
        // so [Authorize(Policy = Permissions.X)] enforces the granted claim.
        services.AddAuthorization(options =>
        {
            foreach (var permission in Permissions.All)
            {
                options.AddPolicy(permission, policy =>
                    policy.AddRequirements(new PermissionRequirement(permission)));
            }
        });

        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        return services;
    }
}
