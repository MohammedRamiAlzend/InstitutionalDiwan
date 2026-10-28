using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Authentication;
using InstitutionalDiwan.Server.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

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
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = context => ValidateSessionAsync(context, jwt.SecretKey)
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

    private static async Task ValidateSessionAsync(TokenValidatedContext context, string signingKey)
    {
        var principal = context.Principal;
        var stamps = principal?.FindAll(JwtSessionStamp.ClaimType).ToArray();
        if (stamps is not { Length: 1 } ||
            !Guid.TryParse(principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            context.Fail("Invalid session.");
            return;
        }

        var services = context.HttpContext.RequestServices;
        var cancellationToken = context.HttpContext.RequestAborted;
        var user = await services.GetRequiredService<IUserRepository>()
            .GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            context.Fail("Invalid session.");
            return;
        }

        var roleRepository = services.GetRequiredService<IRoleRepository>();
        var roles = new List<Role>();
        foreach (var assignment in user.UserRoles)
        {
            var role = await roleRepository.GetByIdAsync(assignment.RoleId, cancellationToken);
            if (role is { IsActive: true })
                roles.Add(role);
        }

        if (!JwtSessionStamp.IsValid(
                stamps[0].Value,
                user,
                roles.Select(role => role.Code),
                roles.SelectMany(role => role.RolePermissions).Select(permission => permission.PermissionCode),
                signingKey))
        {
            // Do not include credentials, tokens, stamps, or account details in failures/logs.
            context.Fail("Invalid session.");
        }
    }
}
