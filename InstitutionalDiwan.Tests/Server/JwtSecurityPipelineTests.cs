using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Features.Identity.Login;
using InstitutionalDiwan.Application.Features.Identity.Users;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Authentication;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Server.Controllers;
using InstitutionalDiwan.Server.Extensions;
using InstitutionalDiwan.Server.Middleware;
using InstitutionalDiwan.Tests.Support;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace InstitutionalDiwan.Tests.Server;

/// <summary>
/// Real Kestrel + JWT bearer + MVC pipeline, real identity handlers/repositories, and a private
/// SQLite database. Every request gets a new persistence scope; no authentication test double.
/// </summary>
public sealed class JwtSecurityPipelineTests
{
    private const string Password = "Original-Test-Password1!";
    private const string NewPassword = "Replacement-Test-Password2!";

    [Theory]
    [InlineData("password-change")]
    [InlineData("deactivate")]
    [InlineData("reactivate")]
    [InlineData("role-revoke")]
    [InlineData("role-deactivate")]
    [InlineData("permission-revoke")]
    [InlineData("scope")]
    [InlineData("primary-department")]
    [InlineData("employee")]
    [InlineData("force-password")]
    [InlineData("profile-timestamp")]
    public async Task PreviouslyValidToken_AfterPersistedSecurityChange_IsUnauthorized(string change)
    {
        await using var host = await SecurityHost.StartAsync();
        var login = await host.LoginAsync();
        await host.AssertGetAsync("/api/auth/me", login.AccessToken, HttpStatusCode.OK);
        await host.AssertGetAsync("/api/security-test/permission", login.AccessToken, HttpStatusCode.OK);

        await host.MutateAsync((user, role) =>
        {
            switch (change)
            {
                case "password-change":
                    // Hash changes must revoke even when the timestamp is identical.
                    user.ChangePasswordHash(new PasswordHasher().Hash(NewPassword), user.UpdatedAt);
                    break;
                case "deactivate": user.Deactivate(); break;
                case "reactivate": user.Deactivate(); user.Activate(); break;
                case "role-revoke": user.RevokeRole(role.RoleId); break;
                case "role-deactivate": role.Deactivate(); break;
                case "permission-revoke": role.RevokePermission(Permissions.UserManage); break;
                case "scope":
                    user.UpdateProfile(user.PrimaryDepartmentId, null, UserScopeType.DepartmentId, false);
                    break;
                case "primary-department":
                    user.UpdateProfile(Guid.NewGuid(), null, user.ScopeTypeId, false);
                    break;
                case "employee":
                    user.UpdateProfile(user.PrimaryDepartmentId, Guid.NewGuid(), user.ScopeTypeId, false);
                    break;
                case "force-password":
                    user.UpdateProfile(user.PrimaryDepartmentId, null, user.ScopeTypeId, true);
                    break;
                case "profile-timestamp":
                    user.UpdateProfile(user.PrimaryDepartmentId, null, user.ScopeTypeId, false,
                        user.UpdatedAt!.Value.AddMilliseconds(1));
                    break;
            }
        });

        await host.AssertGetAsync("/api/auth/me", login.AccessToken, HttpStatusCode.Unauthorized);
        await host.AssertGetAsync("/api/lookups", login.AccessToken, HttpStatusCode.Unauthorized);
        await host.AssertGetAsync("/api/security-test/permission", login.AccessToken, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdministrativeReset_RevokesOldToken_AndForcedSessionCanChangePassword()
    {
        await using var host = await SecurityHost.StartAsync();
        var oldLogin = await host.LoginAsync();
        await using (var scope = host.App.Services.CreateAsyncScope())
        {
            var result = await scope.ServiceProvider.GetRequiredService<ISender>().Send(
                new ResetUserPasswordCommand(oldLogin.UserId, NewPassword));
            Assert.True(result.IsSuccess);
        }

        await host.AssertGetAsync("/api/auth/me", oldLogin.AccessToken, HttpStatusCode.Unauthorized);
        using var deniedChange = await host.ChangePasswordAsync(oldLogin.AccessToken, NewPassword, Password);
        Assert.Equal(HttpStatusCode.Unauthorized, deniedChange.StatusCode);

        var resetLogin = await host.LoginAsync(NewPassword);
        Assert.True(resetLogin.MustChangePassword);
        await host.AssertGetAsync("/api/auth/me", resetLogin.AccessToken, HttpStatusCode.OK);
        await host.AssertGetAsync("/api/lookups", resetLogin.AccessToken, HttpStatusCode.Forbidden);
        using var changed = await host.ChangePasswordAsync(resetLogin.AccessToken, NewPassword, Password);
        Assert.Equal(HttpStatusCode.OK, changed.StatusCode);
        var fresh = (await changed.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.False(fresh.MustChangePassword);
        await host.AssertGetAsync("/api/lookups", fresh.AccessToken, HttpStatusCode.OK);
        await host.AssertGetAsync("/api/auth/me", resetLogin.AccessToken, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ForcedPasswordSession_DeniesOrdinaryAndPermissionRoutes_ButAllowsRecoveryAndAnonymous()
    {
        await using var host = await SecurityHost.StartAsync(mustChangePassword: true);
        var login = await host.LoginAsync();
        await host.AssertGetAsync("/api/auth/me", login.AccessToken, HttpStatusCode.OK);
        await host.AssertGetAsync("/api/auth/me/", login.AccessToken, HttpStatusCode.OK);
        using var lookup = await host.SendAsync(HttpMethod.Get, "/api/lookups", login.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, lookup.StatusCode);
        Assert.Contains("Identity.PasswordChangeRequired", await lookup.Content.ReadAsStringAsync());
        await host.AssertGetAsync("/api/security-test/permission", login.AccessToken, HttpStatusCode.Forbidden);
        // Same path prefix and a different method must not accidentally inherit the exemptions.
        await host.AssertGetAsync("/api/auth/me/extra", login.AccessToken, HttpStatusCode.Forbidden);
        using var wrongMethod = await host.SendAsync(HttpMethod.Post, "/api/auth/me", login.AccessToken);
        Assert.Equal(HttpStatusCode.Forbidden, wrongMethod.StatusCode);
        await host.AssertGetAsync("/api/security-test/anonymous", login.AccessToken, HttpStatusCode.OK);
        var secondLogin = await host.LoginAsync(token: login.AccessToken);
        Assert.True(secondLogin.MustChangePassword);

        using var response = await host.ChangePasswordAsync(login.AccessToken, Password, NewPassword);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var changed = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.False(changed.MustChangePassword);
        await host.AssertGetAsync("/api/lookups", changed.AccessToken, HttpStatusCode.OK);
        await host.AssertGetAsync("/api/security-test/permission", changed.AccessToken, HttpStatusCode.OK);
        await host.AssertGetAsync("/api/auth/me", login.AccessToken, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RecoveryEndpoints_StillRequireAuthentication()
    {
        await using var host = await SecurityHost.StartAsync();
        await host.AssertGetAsync("/api/auth/me", null, HttpStatusCode.Unauthorized);
        using var change = await host.ChangePasswordAsync(null, Password, NewPassword);
        Assert.Equal(HttpStatusCode.Unauthorized, change.StatusCode);
    }

    [Fact]
    public async Task FreshLoginAndPasswordChange_ExcludeInactiveRolesAndTheirPermissions()
    {
        await using var host = await SecurityHost.StartAsync();
        await host.MutateAsync((user, role) => role.Deactivate());
        var login = await host.LoginAsync();
        Assert.Empty(login.Roles);
        Assert.Empty(login.Permissions);
        AssertNoPrivilegeClaims(login.AccessToken);
        await host.AssertGetAsync("/api/auth/me", login.AccessToken, HttpStatusCode.OK);
        await host.AssertGetAsync("/api/security-test/permission", login.AccessToken, HttpStatusCode.Forbidden);
        using var response = await host.ChangePasswordAsync(login.AccessToken, Password, NewPassword);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var changed = (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        Assert.Empty(changed.Roles);
        Assert.Empty(changed.Permissions);
        AssertNoPrivilegeClaims(changed.AccessToken);
        await host.AssertGetAsync("/api/lookups", changed.AccessToken, HttpStatusCode.OK);
    }

    [Fact]
    public async Task AnotherLogin_DoesNotRevokeAnExistingSession()
    {
        await using var host = await SecurityHost.StartAsync();
        var first = await host.LoginAsync();
        // Explicitly move LastLoginAt without touching any security state.
        await host.MutateAsync((user, role) => user.RecordLogin(DateTime.UtcNow.AddMinutes(-1)));
        await host.AssertGetAsync("/api/auth/me", first.AccessToken, HttpStatusCode.OK);
        var second = await host.LoginAsync();
        await host.AssertGetAsync("/api/auth/me", first.AccessToken, HttpStatusCode.OK);
        await host.AssertGetAsync("/api/auth/me", second.AccessToken, HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("malformed")]
    [InlineData("mismatch")]
    [InlineData("duplicate")]
    public async Task SignedTokens_WithoutExactlyOneValidStamp_AreRejected(string kind)
    {
        await using var host = await SecurityHost.StartAsync();
        var login = await host.LoginAsync();
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken);
        var claims = jwt.Claims.Where(claim => claim.Type != JwtSessionStamp.ClaimType).ToList();
        if (kind != "missing")
        {
            claims.Add(new Claim(JwtSessionStamp.ClaimType, kind == "malformed"
                ? new string('!', 44)
                : Convert.ToBase64String(new byte[32])));
        }
        if (kind == "duplicate")
            claims.Add(jwt.Claims.Single(claim => claim.Type == JwtSessionStamp.ClaimType));
        var replacement = new JwtSecurityToken(jwt.Issuer, host.Audience, claims,
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(host.SigningKey)),
                SecurityAlgorithms.HmacSha256));
        var token = new JwtSecurityTokenHandler().WriteToken(replacement);
        await host.AssertGetAsync("/api/auth/me", token, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Stamp_IsCanonicalKeyedAndDoesNotExposeCredentials()
    {
        await using var host = await SecurityHost.StartAsync();
        var login = await host.LoginAsync();
        await using var scope = host.App.Services.CreateAsyncScope();
        var user = (await scope.ServiceProvider.GetRequiredService<IUserRepository>()
            .GetByIdAsync(login.UserId))!;
        var first = JwtSessionStamp.Create(user, ["B", "A", "A"], ["Y", "X"], host.SigningKey);
        Assert.True(JwtSessionStamp.IsValid(first, user, ["A", "B"], ["X", "Y", "X"], host.SigningKey));
        Assert.False(JwtSessionStamp.IsValid(first, user, ["A", "B"], ["X", "Y"], host.SigningKey + "other"));
        var payload = new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken).Payload.SerializeToJson();
        Assert.False(payload.Contains(user.PasswordHash, StringComparison.Ordinal));
        Assert.False(payload.Contains(host.SigningKey, StringComparison.Ordinal));
        Assert.False(payload.Contains(Password, StringComparison.Ordinal));
        Assert.Single(new JwtSecurityTokenHandler().ReadJwtToken(login.AccessToken).Claims,
            claim => claim.Type == JwtSessionStamp.ClaimType);
    }

    [Theory]
    [InlineData(Permissions.DocumentCreate, true, false)]
    [InlineData(Permissions.DocumentEdit, true, true)]
    [InlineData(Permissions.AccessGrantManage, false, true)]
    [InlineData(Permissions.UserManage, true, true)]
    [InlineData(Permissions.DocumentView, true, false)]
    public async Task IdentityOptions_DepartmentsAndUsers_RequireTheirExactPickerPermissions(
        string permission, bool canReadDepartments, bool canReadUsers)
    {
        await using var host = await SecurityHost.StartAsync(operatorPermissions: [permission]);
        var login = await host.LoginAsync();
        await host.AssertGetAsync("/api/identity-options/departments", login.AccessToken,
            canReadDepartments ? HttpStatusCode.OK : HttpStatusCode.Forbidden);

        using var users = await host.SendAsync(HttpMethod.Get, "/api/identity-options/users?pageSize=5",
            login.AccessToken);
        Assert.Equal(canReadUsers ? HttpStatusCode.OK : HttpStatusCode.Forbidden, users.StatusCode);
        if (!canReadUsers) return;
        var body = await users.Content.ReadAsStringAsync();
        Assert.Contains("username", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", body, StringComparison.OrdinalIgnoreCase);
    }

    private static void AssertNoPrivilegeClaims(string token)
    {
        var claims = new JwtSecurityTokenHandler().ReadJwtToken(token).Claims;
        Assert.DoesNotContain(claims, claim => claim.Type is "role" or ClaimTypes.Role or JwtProvider.PermissionClaimType);
    }

    private sealed class SecurityHost : IAsyncDisposable
    {
        private readonly SqliteConnection _connection = new("DataSource=:memory:");
        public string SigningKey { get; } = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        public string Audience => "security-tests";
        public WebApplication App { get; private set; } = null!;
        private HttpClient Client { get; set; } = null!;

        public static async Task<SecurityHost> StartAsync(
            bool mustChangePassword = false, params string[] operatorPermissions)
        {
            var host = new SecurityHost();
            await host._connection.OpenAsync();
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                EnvironmentName = "Testing",
                ApplicationName = typeof(AuthController).Assembly.GetName().Name
            });
            builder.Logging.ClearProviders();
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = host.SigningKey,
                ["Jwt:Issuer"] = "security-tests",
                ["Jwt:Audience"] = host.Audience
            });
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
            builder.Services.AddMediatR(options => options.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly));
            builder.Services.AddDbContext<DiwanDbContext>(options => options.UseSqlite(host._connection));
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IRoleRepository, RoleRepository>();
            builder.Services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            builder.Services.AddScoped<IUnitOfWork>(services => services.GetRequiredService<DiwanDbContext>());
            builder.Services.AddSingleton<IClock>(new TestClock(DateTime.UtcNow));
            builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
            builder.Services.AddScoped<IJwtProvider, JwtProvider>();
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddScoped<IUserContext, UserContext>();
            builder.Services.AddInstitutionalAuthentication(builder.Configuration);
            host.App = builder.Build();
            host.App.UseRouting();
            host.App.UseAuthentication();
            host.App.UseMiddleware<ForcedPasswordChangeMiddleware>();
            host.App.UseAuthorization();
            host.App.MapControllers();
            host.App.MapGet("/api/security-test/permission", () => Results.Ok()).RequireAuthorization(Permissions.UserManage);
            host.App.MapGet("/api/auth/me/extra", () => Results.Ok()).RequireAuthorization();
            host.App.MapPost("/api/auth/me", () => Results.Ok()).RequireAuthorization();
            host.App.MapGet("/api/security-test/anonymous", () => Results.Ok()).AllowAnonymous();

            await using (var scope = host.App.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DiwanDbContext>();
                await db.Database.EnsureCreatedAsync();
                await new UserScopeTypeSeeder().SeedAsync(new SeederContext(db, new FakePasswordHasher(), "Testing"));
                var role = Role.Create("OPERATOR", "مشغل").Value;
                string[] granted = operatorPermissions.Length == 0
                    ? [Permissions.UserManage]
                    : operatorPermissions;
                foreach (var permission in granted)
                {
                    role.GrantPermission(permission);
                    db.Permissions.Add(Permission.Create(permission, permission, "TESTING").Value);
                }
                db.Roles.Add(role);
                var user = User.Create("session-test", new PasswordHasher().Hash(Password), Guid.NewGuid(),
                    scopeTypeId: UserScopeType.SystemId, mustChangePassword: mustChangePassword).Value;
                user.AssignRole(role.RoleId);
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
            await host.App.StartAsync();
            host.Client = new HttpClient { BaseAddress = new Uri(host.App.Urls.Single()) };
            return host;
        }

        public async Task<LoginResponse> LoginAsync(string password = Password, string? token = null)
        {
            using var response = await SendAsync(HttpMethod.Post, "/api/auth/login", token,
                new { username = "session-test", password });
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return (await response.Content.ReadFromJsonAsync<LoginResponse>())!;
        }

        public Task<HttpResponseMessage> ChangePasswordAsync(string? token, string oldPassword, string newPassword) =>
            SendAsync(HttpMethod.Post, "/api/auth/change-password", token, new { oldPassword, newPassword });

        public async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, string? token, object? body = null)
        {
            using var request = new HttpRequestMessage(method, path);
            if (token is not null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body is not null)
                request.Content = JsonContent.Create(body);
            return await Client.SendAsync(request);
        }

        public async Task AssertGetAsync(string path, string? token, HttpStatusCode expected)
        {
            using var response = await SendAsync(HttpMethod.Get, path, token);
            Assert.Equal(expected, response.StatusCode);
        }

        public async Task MutateAsync(Action<User, Role> mutate)
        {
            await using var scope = App.Services.CreateAsyncScope();
            var user = (await scope.ServiceProvider.GetRequiredService<IUserRepository>()
                .GetByUsernameAsync("session-test"))!;
            var role = (await scope.ServiceProvider.GetRequiredService<IRoleRepository>().GetByCodeAsync("OPERATOR"))!;
            mutate(user, role);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
