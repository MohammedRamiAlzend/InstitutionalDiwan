using InstitutionalDiwan.Application;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Infrastructure;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Server.Authorization;
using InstitutionalDiwan.Server.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace InstitutionalDiwan.Tests.Server;

/// <summary>
/// The UI_LABELS_MANAGE policy must accept only the seeded superadmin account: the SUPER_ADMIN
/// role alone is not enough (it can be assigned to other accounts through USER_MANAGE), and the
/// account name alone is not enough either.
/// </summary>
public sealed class UiLabelPolicyTests
{
    private static ServiceProvider BuildProvider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] =
                    "Server=(localdb)\\MSSQLLocalDB;Database=InstitutionalDiwanTests;Trusted_Connection=True;TrustServerCertificate=True",
                ["Jwt:Issuer"] = "InstitutionalDiwan",
                ["Jwt:Audience"] = "InstitutionalDiwan",
                ["Jwt:SecretKey"] = "ui-label-policy-test-signing-key-0123456789",
                ["Jwt:AccessTokenExpirationMinutes"] = "60"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddInstitutionalAuthentication(configuration);

        return services.BuildServiceProvider();
    }

    private static ClaimsPrincipal Principal(string? name, params string[] roles)
    {
        var claims = new List<Claim>();
        if (name is not null)
            claims.Add(new Claim(ClaimTypes.Name, name));
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var identity = new ClaimsIdentity(claims, authenticationType: "Test");
        return new ClaimsPrincipal(identity);
    }

    private static async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal principal)
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        return await authorizationService.AuthorizeAsync(principal, resource: null, UiLabelPolicies.Manage);
    }

    [Fact]
    public async Task Superadmin_role_and_account_name_succeeds()
    {
        var result = await AuthorizeAsync(Principal(SuperAdminSeeder.Username, SuperAdminSeeder.RoleCode));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Superadmin_role_on_another_account_fails()
    {
        var result = await AuthorizeAsync(Principal("admin", SuperAdminSeeder.RoleCode));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Account_name_without_the_role_fails()
    {
        var result = await AuthorizeAsync(Principal(SuperAdminSeeder.Username));

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Every_permission_without_the_superadmin_role_fails()
    {
        var claims = new List<Claim> { new(ClaimTypes.Name, "admin") };
        claims.AddRange(Permissions.All.Select(permission => new Claim("permission", permission)));
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));

        var result = await AuthorizeAsync(principal);

        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Anonymous_fails()
    {
        var result = await AuthorizeAsync(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(result.Succeeded);
    }
}