using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Seed;

public sealed class DepartmentSeederTests
{
    private static SeederContext Context(DiwanDbContext db, string environment = SeedingEnvironments.Development)
        => new(db, new FakePasswordHasher(), environment);

    private static async Task SeedPrerequisitesAsync(DiwanDbContext db)
    {
        await new DepartmentTypeSeeder().SeedAsync(Context(db));
        await new NumberingModeSeeder().SeedAsync(Context(db));
        await new NumberingResetPolicySeeder().SeedAsync(Context(db));
        await new InstitutionSettingSeeder().SeedAsync(Context(db));
    }

    [Fact]
    public async Task SeedAsync_CreatesFourLevelTreeUnderCentralDiwan()
    {
        using var db = TestContextFactory.Create();
        await SeedPrerequisitesAsync(db);

        await new DepartmentSeeder().SeedAsync(Context(db));

        Assert.Equal(15, await db.Departments.CountAsync());

        var root = await db.Departments.SingleAsync(d => d.DepartmentTypeId == DepartmentType.CentralDiwanId);
        Assert.Null(root.ParentDepartmentId);
        Assert.Equal(4, await db.Departments.CountAsync(d => d.DepartmentTypeId == DepartmentType.DirectorateId));
        Assert.Equal(7, await db.Departments.CountAsync(d => d.DepartmentTypeId == DepartmentType.DepartmentId));
        Assert.Equal(3, await db.Departments.CountAsync(d => d.DepartmentTypeId == DepartmentType.SectionId));

        var byCode = await db.Departments.ToDictionaryAsync(d => d.Code);

        Assert.Equal(root.DepartmentId, byCode["DIR-ADMIN"].ParentDepartmentId);
        Assert.Equal(root.DepartmentId, byCode["DIR-IT"].ParentDepartmentId);
        Assert.Equal(root.DepartmentId, byCode["DIR-LEGAL"].ParentDepartmentId);
        Assert.Equal(root.DepartmentId, byCode["DIR-PR"].ParentDepartmentId);

        Assert.Equal(byCode["DIR-ADMIN"].DepartmentId, byCode["DEP-HR"].ParentDepartmentId);
        Assert.Equal(byCode["DIR-ADMIN"].DepartmentId, byCode["DEP-ACCOUNTING"].ParentDepartmentId);
        Assert.Equal(byCode["DIR-ADMIN"].DepartmentId, byCode["DEP-PROCUREMENT"].ParentDepartmentId);
        Assert.Equal(byCode["DIR-IT"].DepartmentId, byCode["DEP-SYS-DEV"].ParentDepartmentId);
        Assert.Equal(byCode["DIR-IT"].DepartmentId, byCode["DEP-INFRA"].ParentDepartmentId);
        Assert.Equal(byCode["DIR-LEGAL"].DepartmentId, byCode["DEP-CONTRACTS"].ParentDepartmentId);
        Assert.Equal(byCode["DIR-PR"].DepartmentId, byCode["DEP-MEDIA"].ParentDepartmentId);

        Assert.Equal(byCode["DEP-HR"].DepartmentId, byCode["SEC-PAYROLL"].ParentDepartmentId);
        Assert.Equal(byCode["DEP-HR"].DepartmentId, byCode["SEC-PERSONNEL"].ParentDepartmentId);
        Assert.Equal(byCode["DEP-ACCOUNTING"].DepartmentId, byCode["SEC-BUDGET"].ParentDepartmentId);

        // Four levels: root → directorate → department → section.
        var depth = 0;
        var current = byCode["SEC-PAYROLL"];
        while (current.ParentDepartmentId is not null)
        {
            depth++;
            var parentId = current.ParentDepartmentId.Value;
            current = byCode.Values.Single(d => d.DepartmentId == parentId);
        }

        Assert.Equal(3, depth);

        Assert.Equal("مديرية الشؤون الإدارية والمالية", byCode["DIR-ADMIN"].NameAr);
        Assert.Equal("قسم الموارد البشرية", byCode["DEP-HR"].NameAr);
        Assert.Equal("شعبة الأجور والتعويضات", byCode["SEC-PAYROLL"].NameAr);
        Assert.All(byCode.Values, d => Assert.True(d.IsActive));

        Assert.True(byCode["DIR-PR"].CanSendExternal);
        Assert.True(byCode["DIR-PR"].CanReceiveExternal);
        Assert.False(byCode["DIR-IT"].CanSendExternal);
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        using var db = TestContextFactory.Create();
        await SeedPrerequisitesAsync(db);

        var seeder = new DepartmentSeeder();
        await seeder.SeedAsync(Context(db));
        var idsAfterFirstRun = await db.Departments
            .Select(d => new { d.Code, d.DepartmentId })
            .OrderBy(d => d.Code)
            .ToListAsync();

        await seeder.SeedAsync(Context(db));

        Assert.Equal(15, await db.Departments.CountAsync());
        Assert.Equal(
            idsAfterFirstRun,
            await db.Departments
                .Select(d => new { d.Code, d.DepartmentId })
                .OrderBy(d => d.Code)
                .ToListAsync());
    }

    [Fact]
    public async Task SeedAsync_DoesNotOverwriteExistingDepartment()
    {
        using var db = TestContextFactory.Create();
        await SeedPrerequisitesAsync(db);

        var root = await db.Departments.SingleAsync(d => d.DepartmentTypeId == DepartmentType.CentralDiwanId);
        var existing = Department.Create(
            "DIR-ADMIN", "مديرية مخصصة", DepartmentType.DirectorateId, root.DepartmentId, nameEn: "Custom Directorate");
        Assert.True(existing.IsSuccess);
        db.Departments.Add(existing.Value);
        await db.SaveChangesAsync();

        await new DepartmentSeeder().SeedAsync(Context(db));

        var directorate = await db.Departments.SingleAsync(d => d.Code == "DIR-ADMIN");
        Assert.Equal("مديرية مخصصة", directorate.NameAr);
        Assert.Equal(15, await db.Departments.CountAsync());

        var humanResources = await db.Departments.SingleAsync(d => d.Code == "DEP-HR");
        Assert.Equal(directorate.DepartmentId, humanResources.ParentDepartmentId);
    }

    [Fact]
    public async Task SeedAsync_ThrowsWhenCentralDiwanIsMissing()
    {
        using var db = TestContextFactory.Create();
        await new DepartmentTypeSeeder().SeedAsync(Context(db));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new DepartmentSeeder().SeedAsync(Context(db)));

        Assert.Contains("InstitutionSettingSeeder", exception.Message);
        Assert.Empty(db.Departments);
    }

    [Fact]
    public void DemoTree_IsGatedToDevelopmentAndStaging()
    {
        var seeder = new DepartmentSeeder();

        Assert.True(seeder.RunsIn(SeedingEnvironments.Development));
        Assert.True(seeder.RunsIn(SeedingEnvironments.Staging));
        Assert.False(seeder.RunsIn(SeedingEnvironments.Production));
        Assert.DoesNotContain(SeedingEnvironments.Production, seeder.Environments);
    }
}
