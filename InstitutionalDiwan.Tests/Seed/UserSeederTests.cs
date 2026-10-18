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

    [Fact]
    public async Task SeedAsync_CreatesDemoAccountsWithRoleAssignments()
    {
        using var db = TestContextFactory.Create();
        await SeedRolesAsync(db);

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
        await SeedRolesAsync(db);

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
        await SeedRolesAsync(db);

        var preExisting = User.Create("admin", "custom-hash", Guid.NewGuid(), scopeTypeId: UserScopeType.SystemId, mustChangePassword: false);
        Assert.True(preExisting.IsSuccess);
        db.Users.Add(preExisting.Value);
        await db.SaveChangesAsync();

        await new UserSeeder().SeedAsync(Context(db));

        var admin = await db.Users.SingleAsync(u => u.Username == "admin");
        Assert.Equal("custom-hash", admin.PasswordHash);
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
