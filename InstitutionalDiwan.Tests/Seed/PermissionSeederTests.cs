using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Seed;

public sealed class PermissionSeederTests
{
    private static SeederContext Context(DiwanDbContext db)
        => new(db, new FakePasswordHasher(), SeedingEnvironments.Development);

    [Fact]
    public async Task SeedAsync_InsertsFullPermissionCatalog()
    {
        using var db = TestContextFactory.Create();

        await new PermissionSeeder().SeedAsync(Context(db));

        var codes = await db.Permissions.Select(p => p.Code).ToListAsync();
        Assert.Equal(13, codes.Count);
        Assert.Contains("DOCUMENT_VIEW", codes);
        Assert.Contains("DOCUMENT_APPROVE", codes);
        Assert.Contains("NUMBERING_VIEW", codes);
        Assert.Contains("DEPARTMENT_MANAGE", codes);
        Assert.Contains("ROLE_MANAGE", codes);
        Assert.Contains("USER_MANAGE", codes);
        Assert.Contains("EMPLOYEE_MANAGE", codes);
        Assert.Contains("ACCESS_GRANT_MANAGE", codes);
        Assert.Contains("REPORT_VIEW", codes);
        Assert.Contains("LOOKUP_MANAGE", codes);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        using var db = TestContextFactory.Create();
        var seeder = new PermissionSeeder();

        await seeder.SeedAsync(Context(db));
        await seeder.SeedAsync(Context(db));

        Assert.Equal(13, await db.Permissions.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_LeavesExistingPermissionsUntouched()
    {
        using var db = TestContextFactory.Create();
        var existing = Permission.Create("DOCUMENT_VIEW", "عرض المستندات", "CORRESPONDENCE");
        Assert.True(existing.IsSuccess);
        db.Permissions.Add(existing.Value);
        await db.SaveChangesAsync();

        await new PermissionSeeder().SeedAsync(Context(db));

        Assert.Equal(13, await db.Permissions.CountAsync());
        Assert.Equal(1, await db.Permissions.CountAsync(p => p.Code == "DOCUMENT_VIEW"));
    }
}
