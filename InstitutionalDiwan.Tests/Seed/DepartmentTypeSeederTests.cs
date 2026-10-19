using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Seed;

public sealed class DepartmentTypeSeederTests
{
    private static SeederContext Context(DiwanDbContext db)
        => new(db, new FakePasswordHasher(), SeedingEnvironments.Development);

    [Fact]
    public async Task SeedAsync_InsertsDefaultDepartmentTypes()
    {
        using var db = TestContextFactory.Create();

        await new DepartmentTypeSeeder().SeedAsync(Context(db));

        var ids = await db.DepartmentTypes.Select(type => type.Id).ToListAsync();
        Assert.Equal(5, ids.Count);
        Assert.Contains(DepartmentType.CentralDiwanId, ids);
        Assert.Contains(DepartmentType.DirectorateId, ids);
        Assert.Contains(DepartmentType.DepartmentId, ids);
        Assert.Contains(DepartmentType.SectionId, ids);
        Assert.Contains(DepartmentType.UnitId, ids);

        var section = await db.DepartmentTypes.SingleAsync(type => type.Id == DepartmentType.SectionId);
        Assert.Equal("Section", section.Desc);
        Assert.Equal("شعبة", section.DescAr);
        Assert.True(section.IsActive);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        using var db = TestContextFactory.Create();
        var seeder = new DepartmentTypeSeeder();

        await seeder.SeedAsync(Context(db));
        await seeder.SeedAsync(Context(db));

        Assert.Equal(5, await db.DepartmentTypes.CountAsync());
    }

    [Fact]
    public async Task SeedAsync_LeavesExistingRowsUntouched()
    {
        using var db = TestContextFactory.Create();
        var existing = DepartmentType.Create(
            DepartmentType.SectionId, "Custom Section", "شعبة مخصصة", hierarchyLevelOrder: 9);
        Assert.True(existing.IsSuccess);
        db.DepartmentTypes.Add(existing.Value);
        await db.SaveChangesAsync();

        await new DepartmentTypeSeeder().SeedAsync(Context(db));

        Assert.Equal(5, await db.DepartmentTypes.CountAsync());
        var section = await db.DepartmentTypes.SingleAsync(type => type.Id == DepartmentType.SectionId);
        Assert.Equal("Custom Section", section.Desc);
        Assert.Equal(9, section.HierarchyLevelOrder);
    }
}
