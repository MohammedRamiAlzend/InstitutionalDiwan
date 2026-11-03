using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Seed;

public sealed class UserSeederTests
{
    private static SeederContext Context(DiwanDbContext db, string environment = SeedingEnvironments.Development)
        => new(db, new FakePasswordHasher(), environment);

    private static async Task SeedRolesAsync(DiwanDbContext db)
    {
        await new UserScopeTypeSeeder().SeedAsync(Context(db));
        await new RoleSeeder().SeedAsync(Context(db));
    }

    private static async Task SeedPrerequisitesAsync(DiwanDbContext db)
    {
        await SeedRolesAsync(db);
        await new DepartmentTypeSeeder().SeedAsync(Context(db));
        await new NumberingModeSeeder().SeedAsync(Context(db));
        await new NumberingResetPolicySeeder().SeedAsync(Context(db));
        await new InstitutionSettingSeeder().SeedAsync(Context(db));
        await new DepartmentSeeder().SeedAsync(Context(db));
    }

    [Fact]
    public async Task SeedAsync_PlacesDemoAccountsInRealDepartments()
    {
        using var db = TestContextFactory.Create();
        await SeedPrerequisitesAsync(db);

        await new UserSeeder().SeedAsync(Context(db));

        var departments = await db.Departments.ToDictionaryAsync(d => d.Code, d => d.DepartmentId);
        var admin = await db.Users.SingleAsync(u => u.Username == "admin");
        var user = await db.Users.SingleAsync(u => u.Username == "user");

        Assert.Equal(departments["CENTRAL_DIWAN"], admin.PrimaryDepartmentId);
        Assert.Equal(departments["DEP-HR"], user.PrimaryDepartmentId);
    }

    [Fact]
    public async Task SeedAsync_ThrowsWhenDepartmentsAreMissingForNewAccount()
    {
        using var db = TestContextFactory.Create();
        await SeedRolesAsync(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new UserSeeder().SeedAsync(Context(db)));

        Assert.Contains("InstitutionSettingSeeder", exception.Message);
        Assert.Equal(0, await db.Users.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_LeavesExistingAccountsAloneWhenDepartmentsAreMissing()
    {
        using var db = TestContextFactory.Create();
        await SeedRolesAsync(db);

        var originalDepartmentId = Guid.NewGuid();
        foreach (var (username, scopeTypeId) in new[] { ("admin", UserScopeType.SystemId), ("user", UserScopeType.DepartmentId) })
        {
            var existing = User.Create(username, "custom-hash", originalDepartmentId, scopeTypeId: scopeTypeId, mustChangePassword: false);
            Assert.True(existing.IsSuccess);
            db.Users.Add(existing.Value);
        }
        await db.SaveChangesAsync();

        await new UserSeeder().SeedAsync(Context(db));

        Assert.Equal(2, await db.Users.CountAsync(u => u.PrimaryDepartmentId == originalDepartmentId));
    }

    [Fact]
    public async Task SeedAsync_CreatesDemoAccountsWithRoleAssignments()
    {
        using var db = TestContextFactory.Create();
        await SeedPrerequisitesAsync(db);

        await new UserSeeder().SeedAsync(Context(db));

        var admin = await db.Users.SingleAsync(u => u.Username == "admin");
        var user = await db.Users.SingleAsync(u => u.Username == "user");

        var adminRoleId = (await db.Roles.SingleAsync(r => r.Code == "ADMIN")).RoleId;
        var userRoleId = (await db.Roles.SingleAsync(r => r.Code == "USER")).RoleId;

        Assert.True(await db.UserRoles.AnyAsync(ur => ur.UserId == admin.UserId && ur.RoleId == adminRoleId));
        Assert.True(await db.UserRoles.AnyAsync(ur => ur.UserId == user.UserId && ur.RoleId == userRoleId));

        var hasher = new FakePasswordHasher();
        Assert.True(hasher.Verify(UserSeeder.DemoPassword, admin.PasswordHash));
        Assert.True(hasher.Verify(UserSeeder.DemoPassword, user.PasswordHash));
        Assert.True(admin.IsActive);
        Assert.False(admin.MustChangePassword);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        using var db = TestContextFactory.Create();
        var seeder = new UserSeeder();
        await SeedPrerequisitesAsync(db);

        await seeder.SeedAsync(Context(db));
        var userIdsAfterFirstRun = await db.Users.Select(u => u.UserId).OrderBy(id => id).ToListAsync();

        await seeder.SeedAsync(Context(db));

        Assert.Equal(2, await db.Users.CountAsync());
        Assert.Equal(userIdsAfterFirstRun, await db.Users.Select(u => u.UserId).OrderBy(id => id).ToListAsync());
    }

    [Fact]
    public async Task SeedAsync_DoesNotOverwriteExistingUser()
    {
        using var db = TestContextFactory.Create();
        await SeedPrerequisitesAsync(db);

        var originalDepartmentId = Guid.NewGuid();
        var preExisting = User.Create("admin", "custom-hash", originalDepartmentId, scopeTypeId: UserScopeType.SystemId, mustChangePassword: false);
        Assert.True(preExisting.IsSuccess);
        db.Users.Add(preExisting.Value);
        await db.SaveChangesAsync();

        await new UserSeeder().SeedAsync(Context(db));

        var admin = await db.Users.SingleAsync(u => u.Username == "admin");
        Assert.Equal("custom-hash", admin.PasswordHash);
        Assert.Equal(originalDepartmentId, admin.PrimaryDepartmentId);
        Assert.Equal(2, await db.Users.CountAsync());
        Assert.True(await db.Users.AnyAsync(u => u.Username == "user"));
    }

    [Fact]
    public async Task SeedAsync_ThrowsWhenSystemRolesAreMissing()
    {
        using var db = TestContextFactory.Create();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new UserSeeder().SeedAsync(Context(db)));

        Assert.Contains("RoleSeeder", exception.Message);
    }

    [Fact]
    public void DemoUsers_AreGatedToDevelopmentAndStaging()
    {
        var seeder = new UserSeeder();

        Assert.True(seeder.RunsIn(SeedingEnvironments.Development));
        Assert.True(seeder.RunsIn(SeedingEnvironments.Staging));
        Assert.False(seeder.RunsIn(SeedingEnvironments.Production));
        Assert.DoesNotContain(SeedingEnvironments.Production, seeder.Environments);
    }
}
