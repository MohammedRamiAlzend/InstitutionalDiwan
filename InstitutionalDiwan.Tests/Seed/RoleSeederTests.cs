using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Seed;

public sealed class RoleSeederTests
{
    private static SeederContext Context(DiwanDbContext db)
        => new(db, new FakePasswordHasher(), SeedingEnvironments.Development);

    [Fact]
    public async Task SeedAsync_CreatesSystemRolesWithExpectedGrants()
    {
        using var db = TestContextFactory.Create();

        await new RoleSeeder().SeedAsync(Context(db));

        var admin = await db.Roles.SingleAsync(r => r.Code == "ADMIN");
        var user = await db.Roles.SingleAsync(r => r.Code == "USER");

        Assert.True(admin.IsSystemRole);
        Assert.False(user.IsSystemRole);
        Assert.True(admin.IsActive);
        Assert.True(user.IsActive);

        var adminGrants = await db.RolePermissions
            .Where(rp => rp.RoleId == admin.RoleId)
            .Select(rp => rp.PermissionCode)
            .ToListAsync();
        var userGrants = await db.RolePermissions
            .Where(rp => rp.RoleId == user.RoleId)
            .Select(rp => rp.PermissionCode)
            .ToListAsync();

        Assert.Equal(14, adminGrants.Count);
        Assert.Equal(3, userGrants.Count);
        Assert.Contains("USER_MANAGE", adminGrants);
        Assert.Contains("EMPLOYEE_MANAGE", adminGrants);
        Assert.Contains("ACCESS_GRANT_MANAGE", adminGrants);
        Assert.Contains("DOCUMENT_APPROVE", adminGrants);
        Assert.Contains("NUMBERING_VIEW", adminGrants);
        Assert.Contains("NUMBERING_MANAGE", adminGrants);
        Assert.Contains("LOOKUP_MANAGE", adminGrants);
        Assert.DoesNotContain("USER_MANAGE", userGrants);
        Assert.Equal(userGrants.OrderBy(c => c), new[] { "DOCUMENT_CREATE", "DOCUMENT_EDIT", "DOCUMENT_VIEW" }.OrderBy(c => c));
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        using var db = TestContextFactory.Create();
        var seeder = new RoleSeeder();

        await seeder.SeedAsync(Context(db));
        var adminIdAfterFirstRun = (await db.Roles.SingleAsync(r => r.Code == "ADMIN")).RoleId;

        await seeder.SeedAsync(Context(db));

        Assert.Equal(2, await db.Roles.CountAsync());
        Assert.Equal(17, await db.RolePermissions.CountAsync());
        Assert.Equal(adminIdAfterFirstRun, (await db.Roles.SingleAsync(r => r.Code == "ADMIN")).RoleId);
    }

    [Fact]
    public async Task SeedAsync_AddsMissingGrantsToExistingRole()
    {
        using var db = TestContextFactory.Create();
        var existingAdmin = Role.Create("ADMIN", "مسؤول إداري", "Administrator", "Full system access", isSystemRole: true);
        Assert.True(existingAdmin.IsSuccess);
        db.Roles.Add(existingAdmin.Value);
        await db.SaveChangesAsync();

        await new RoleSeeder().SeedAsync(Context(db));

        var admin = await db.Roles.SingleAsync(r => r.Code == "ADMIN");
        Assert.Equal(14, await db.RolePermissions.CountAsync(rp => rp.RoleId == admin.RoleId));
        Assert.Equal(2, await db.Roles.CountAsync());
    }
}
