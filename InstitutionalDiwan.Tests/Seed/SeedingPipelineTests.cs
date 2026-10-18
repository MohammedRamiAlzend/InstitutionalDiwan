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
        new UserScopeTypeSeeder(),
        new AssignmentTypeSeeder(),
        new AccessLevelSeeder(),
        new ParticipantRoleSeeder(),
        new ParticipantTargetTypeSeeder(),
        new OrganizationTypeSeeder(),
        new MovementTypeSeeder(),
        new MovementStatusSeeder(),
        new MovementStepStatusSeeder(),
        new MovementEventTypeSeeder(),
        new StorageProviderTypeSeeder(),
        new DocumentFileTypeSeeder(),
        new FileStorageProviderSeeder(),
        new DocumentRelationTypeSeeder(),
        new RelationActionTypeSeeder(),
        new ArchiveEventTypeSeeder(),
        new ArchiveLocationSeeder(),
        new AuditActionTypeSeeder(),
        new AuditEntityTypeSeeder(),
        new NotificationTypeSeeder(),
        new NotificationPrioritySeeder(),
        new NotificationChannelSeeder(),
        new NotificationOutboxStatusSeeder(),
        new NumberingModeSeeder(),
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

        Assert.Equal(36, executed);

        // Reference lookups
        Assert.Equal(4, await db.UserScopeTypes.CountAsync());
        Assert.Equal(2, await db.AssignmentTypes.CountAsync());
        Assert.Equal(4, await db.AccessLevels.CountAsync());
        Assert.Equal(4, await db.ParticipantRoles.CountAsync());
        Assert.Equal(3, await db.ParticipantTargetTypes.CountAsync());
        Assert.Equal(4, await db.OrganizationTypes.CountAsync());
        Assert.Equal(4, await db.MovementTypes.CountAsync());
        Assert.Equal(10, await db.MovementStatuses.CountAsync());
        Assert.Equal(7, await db.MovementStepStatuses.CountAsync());
        Assert.Equal(9, await db.MovementEventTypes.CountAsync());
        Assert.Equal(4, await db.StorageProviderTypes.CountAsync());
        Assert.Equal(4, await db.DocumentFileTypes.CountAsync());
        Assert.Equal(1, await db.FileStorageProviders.CountAsync());
        Assert.Equal(4, await db.DocumentRelationTypes.CountAsync());
        Assert.Equal(3, await db.RelationActionTypes.CountAsync());
        Assert.Equal(3, await db.ArchiveEventTypes.CountAsync());
        Assert.Equal(3, await db.ArchiveLocations.CountAsync());
        Assert.Equal(7, await db.AuditActionTypes.CountAsync());
        Assert.Equal(8, await db.AuditEntityTypes.CountAsync());
        Assert.Equal(6, await db.NotificationTypes.CountAsync());
        Assert.Equal(3, await db.NotificationPriorities.CountAsync());
        Assert.Equal(3, await db.NotificationChannels.CountAsync());
        Assert.Equal(4, await db.NotificationOutboxStatuses.CountAsync());
        Assert.Equal(2, await db.NumberingModes.CountAsync());

        // Module lookups, permissions, roles, and demo users
        Assert.Equal(4, await db.DepartmentTypes.CountAsync());
        Assert.Equal(4, await db.DocumentDirections.CountAsync());
        Assert.Equal(5, await db.DocumentTypes.CountAsync());
        Assert.Equal(5, await db.DocumentStatusTypes.CountAsync());
        Assert.Equal(4, await db.DocumentConfidentialityLevels.CountAsync());
        Assert.Equal(3, await db.DocumentPriorityLevels.CountAsync());
        Assert.Equal(3, await db.DocumentNumberStatuses.CountAsync());
        Assert.Equal(3, await db.DocumentNumberEventTypes.CountAsync());
        Assert.Equal(3, await db.NumberingResetPolicies.CountAsync());
        Assert.Equal(12, await db.Permissions.CountAsync());
        Assert.Equal(2, await db.Roles.CountAsync());
        Assert.Equal(15, await db.RolePermissions.CountAsync());
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Equal(2, await db.UserRoles.CountAsync());
    }

    [Fact]
    public async Task ProductionRun_SkipsDemoUsersButSeedsLookupsRolesAndPermissions()
    {
        using var db = TestContextFactory.Create();

        var executed = await RunAsync(db, SeedingEnvironments.Production);

        Assert.Equal(35, executed);
        Assert.Equal(4, await db.DepartmentTypes.CountAsync());
        Assert.Equal(4, await db.DocumentDirections.CountAsync());
        Assert.Equal(5, await db.DocumentTypes.CountAsync());
        Assert.Equal(5, await db.DocumentStatusTypes.CountAsync());
        Assert.Equal(4, await db.UserScopeTypes.CountAsync());
        Assert.Equal(6, await db.NotificationTypes.CountAsync());
        Assert.Equal(12, await db.Permissions.CountAsync());
        Assert.Equal(2, await db.Roles.CountAsync());
        Assert.Equal(15, await db.RolePermissions.CountAsync());
        Assert.Equal(0, await db.Users.CountAsync());
    }

    [Fact]
    public async Task RepeatedRuns_AreIdempotent()
    {
        using var db = TestContextFactory.Create();
        await RunAsync(db, SeedingEnvironments.Development);
        var usersAfterFirstRun = await db.Users.Select(u => u.UserId).OrderBy(id => id).ToListAsync();

        var executed = await RunAsync(db, SeedingEnvironments.Development);

        Assert.Equal(36, executed);
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Equal(4, await db.DepartmentTypes.CountAsync());
        Assert.Equal(4, await db.UserScopeTypes.CountAsync());
        Assert.Equal(5, await db.DocumentTypes.CountAsync());
        Assert.Equal(7, await db.MovementStepStatuses.CountAsync());
        Assert.Equal(12, await db.Permissions.CountAsync());
        Assert.Equal(15, await db.RolePermissions.CountAsync());
        Assert.Equal(usersAfterFirstRun, await db.Users.Select(u => u.UserId).OrderBy(id => id).ToListAsync());
    }
}
