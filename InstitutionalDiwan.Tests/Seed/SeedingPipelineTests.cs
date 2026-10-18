using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Seed;

/// <summary>End-to-end seeding pipeline tests: the real seeders through the real runner.</summary>
public sealed class SeedingPipelineTests
{
    private static readonly ISeeder[] AllSeeders =
    {
        new DepartmentTypeSeeder(),
        new DocumentDirectionSeeder(),
        new DocumentTypeSeeder(),
        new DocumentStatusTypeSeeder(),
        new DocumentConfidentialityLevelSeeder(),
        new DocumentPriorityLevelSeeder(),
        new DocumentNumberStatusSeeder(),
        new DocumentNumberEventTypeSeeder(),
        new NumberingResetPolicySeeder(),
        new PermissionSeeder(),
        new RoleSeeder(),
        new UserSeeder(),
    };

    private static Task<int> RunAsync(DiwanDbContext db, string environment)
        => SeederRunner.RunAsync(AllSeeders, new SeederContext(db, new FakePasswordHasher(), environment));

    [Fact]
    public void TestContext_UsesConfiguredProvider()
    {
        // Regression guard: DiwanDbContext.OnConfiguring must not override configured options
        // with its design-time SQL Server fallback (tests configure SQLite themselves).
        using var db = TestContextFactory.Create();

        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", db.Database.ProviderName);
    }

    [Fact]
    public async Task DevelopmentRun_SeedsEverything()
    {
        using var db = TestContextFactory.Create();

        var executed = await RunAsync(db, SeedingEnvironments.Development);

        Assert.Equal(12, executed);
        Assert.Equal(4, await db.DepartmentTypes.CountAsync());
        Assert.Equal(4, await db.DocumentDirections.CountAsync());
        Assert.Equal(5, await db.DocumentTypes.CountAsync());
        Assert.Equal(5, await db.DocumentStatusTypes.CountAsync());
        Assert.Equal(4, await db.DocumentConfidentialityLevels.CountAsync());
        Assert.Equal(3, await db.DocumentPriorityLevels.CountAsync());
        Assert.Equal(3, await db.DocumentNumberStatuses.CountAsync());
        Assert.Equal(3, await db.DocumentNumberEventTypes.CountAsync());
        Assert.Equal(3, await db.NumberingResetPolicies.CountAsync());
        Assert.Equal(10, await db.Permissions.CountAsync());
        Assert.Equal(2, await db.Roles.CountAsync());
        Assert.Equal(13, await db.RolePermissions.CountAsync());
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Equal(2, await db.UserRoles.CountAsync());
    }

    [Fact]
    public async Task ProductionRun_SkipsDemoUsersButSeedsLookupsRolesAndPermissions()
    {
        using var db = TestContextFactory.Create();

        var executed = await RunAsync(db, SeedingEnvironments.Production);

        Assert.Equal(11, executed);
        Assert.Equal(4, await db.DepartmentTypes.CountAsync());
        Assert.Equal(4, await db.DocumentDirections.CountAsync());
        Assert.Equal(5, await db.DocumentTypes.CountAsync());
        Assert.Equal(5, await db.DocumentStatusTypes.CountAsync());
        Assert.Equal(10, await db.Permissions.CountAsync());
        Assert.Equal(2, await db.Roles.CountAsync());
        Assert.Equal(13, await db.RolePermissions.CountAsync());
        Assert.Equal(0, await db.Users.CountAsync());
    }

    [Fact]
    public async Task RepeatedRuns_AreIdempotent()
    {
        using var db = TestContextFactory.Create();
        await RunAsync(db, SeedingEnvironments.Development);
        var usersAfterFirstRun = await db.Users.Select(u => u.UserId).OrderBy(id => id).ToListAsync();

        var executed = await RunAsync(db, SeedingEnvironments.Development);

        Assert.Equal(12, executed);
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Equal(4, await db.DepartmentTypes.CountAsync());
        Assert.Equal(5, await db.DocumentTypes.CountAsync());
        Assert.Equal(10, await db.Permissions.CountAsync());
        Assert.Equal(13, await db.RolePermissions.CountAsync());
        Assert.Equal(usersAfterFirstRun, await db.Users.Select(u => u.UserId).OrderBy(id => id).ToListAsync());
    }
}
