using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Seed;

/// <summary>The three seeding modes (Everything / Standard / None) through the real seeders.</summary>
public sealed class SeedingModeTests
{
    private static readonly ISeeder[] Seeders =
    {
        new UserScopeTypeSeeder(),
        new DepartmentTypeSeeder(),
        new NumberingModeSeeder(),
        new NumberingResetPolicySeeder(),
        new PermissionSeeder(),
        new RoleSeeder(),
        new InstitutionSettingSeeder(),
        new DepartmentSeeder(),
        new EmployeeSeeder(),
        new UserSeeder(),
        new StandardDepartmentSeeder(),
        new SuperAdminSeeder(),
        new StandardAccountSeeder(),
    };

    private static Task<int> RunAsync(DiwanDbContext db, string mode, string? password = null)
        => SeederRunner.RunAsync(
            Seeders,
            new SeederContext(
                db,
                new FakePasswordHasher(),
                SeedingEnvironments.Development,
                SeedingOptions.From(mode, password)));

    [Fact]
    public void Parse_IsCaseInsensitiveDefaultsToEverythingAndRejectsUnknown()
    {
        Assert.Equal(SeedingModes.Everything, SeedingModes.Parse(null));
        Assert.Equal(SeedingModes.Everything, SeedingModes.Parse("  "));
        Assert.Equal(SeedingModes.Standard, SeedingModes.Parse("standard"));
        Assert.Equal(SeedingModes.None, SeedingModes.Parse("NONE"));
        Assert.Throws<InvalidOperationException>(() => SeedingModes.Parse("Demo"));
    }

    [Fact]
    public async Task StandardMode_SeedsThreeDiwansSuperAdminAndSixAccounts()
    {
        using var db = TestContextFactory.Create();

        await RunAsync(db, SeedingModes.Standard);

        var departments = await db.Departments.Select(d => d.NameAr).ToListAsync();
        Assert.Equal(3, departments.Count);
        Assert.Contains("الديوان المركزي", departments);
        Assert.Contains("ديوان الفرد", departments);
        Assert.Contains("ديوان التحقيق", departments);

        var usernames = await db.Users.Select(u => u.Username).ToListAsync();
        Assert.Equal(7, usernames.Count);
        foreach (var expected in new[]
                 {
                     "superadmin", "الديوان المركزي", "احمد ديوان مركزي", "ديوان الفرد",
                     "احمد ديوان الفرد", "ديوان التحقيق", "احمد ديوان التحقيق",
                 })
            Assert.Contains(expected, usernames);

        Assert.All(await db.Users.ToListAsync(), u => Assert.Equal("fake:12345678", u.PasswordHash));
        Assert.Equal(6, await db.Employees.CountAsync());
        Assert.DoesNotContain("admin", usernames);
    }

    [Fact]
    public async Task StandardMode_GivesSuperAdminEveryPermission()
    {
        using var db = TestContextFactory.Create();

        await RunAsync(db, SeedingModes.Standard);

        var roleId = (await db.Roles.SingleAsync(r => r.Code == SuperAdminSeeder.RoleCode)).RoleId;
        var granted = await db.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .Select(rp => rp.PermissionCode)
            .ToListAsync();
        Assert.Equal(Permissions.All.Count, granted.Count);
        Assert.True(Permissions.All.All(code => granted.Contains(code)));

        var superAdmin = await db.Users.Include(u => u.UserRoles).SingleAsync(u => u.Username == "superadmin");
        Assert.Equal(UserScopeType.SystemId, superAdmin.ScopeTypeId);
        Assert.Single(superAdmin.UserRoles);
    }

    [Fact]
    public async Task NoneMode_SeedsOnlyTheSuperAdminAccount()
    {
        using var db = TestContextFactory.Create();

        await RunAsync(db, SeedingModes.None, "Secret-123");

        var user = await db.Users.SingleAsync();
        Assert.Equal("superadmin", user.Username);
        Assert.Equal("fake:Secret-123", user.PasswordHash);
        Assert.Equal(1, await db.Departments.CountAsync());
        Assert.Equal(0, await db.Employees.CountAsync());
        Assert.True(await db.Permissions.AnyAsync());
    }

    [Fact]
    public async Task EverythingMode_KeepsTheDemoDataAndSkipsStandardAccounts()
    {
        using var db = TestContextFactory.Create();

        await RunAsync(db, SeedingModes.Everything);

        var usernames = await db.Users.Select(u => u.Username).ToListAsync();
        Assert.Contains("admin", usernames);
        Assert.Contains("user", usernames);
        Assert.DoesNotContain("superadmin", usernames);
        Assert.DoesNotContain("ديوان الفرد", usernames);
        Assert.True(await db.Departments.CountAsync() > 3);
    }

    [Theory]
    [InlineData(SeedingModes.Standard)]
    [InlineData(SeedingModes.None)]
    public async Task Modes_AreIdempotent(string mode)
    {
        using var db = TestContextFactory.Create();

        await RunAsync(db, mode);
        var users = await db.Users.CountAsync();
        var departments = await db.Departments.CountAsync();
        var grants = await db.RolePermissions.CountAsync();

        await RunAsync(db, mode);

        Assert.Equal(users, await db.Users.CountAsync());
        Assert.Equal(departments, await db.Departments.CountAsync());
        Assert.Equal(grants, await db.RolePermissions.CountAsync());
    }
}
