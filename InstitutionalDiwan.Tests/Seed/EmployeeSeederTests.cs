using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Seed;

public sealed class EmployeeSeederTests
{
    private static SeederContext Context(DiwanDbContext db, string environment = SeedingEnvironments.Development)
        => new(db, new FakePasswordHasher(), environment);

    private static async Task SeedPrerequisitesAsync(DiwanDbContext db)
    {
        await new DepartmentTypeSeeder().SeedAsync(Context(db));
        await new NumberingModeSeeder().SeedAsync(Context(db));
        await new NumberingResetPolicySeeder().SeedAsync(Context(db));
        await new InstitutionSettingSeeder().SeedAsync(Context(db));
        await new DepartmentSeeder().SeedAsync(Context(db));
    }

    [Fact]
    public async Task SeedAsync_AttachesEmployeesToTheirDepartments()
    {
        using var db = TestContextFactory.Create();
        await SeedPrerequisitesAsync(db);

        await new EmployeeSeeder().SeedAsync(Context(db));

        Assert.Equal(28, await db.Employees.CountAsync());
        Assert.All(await db.Employees.ToListAsync(), employee => Assert.True(employee.IsActive));

        var departments = await db.Departments.ToDictionaryAsync(department => department.Code);
        var byNumber = await db.Employees.ToDictionaryAsync(employee => employee.EmployeeNumber!);

        Assert.Equal(departments["DIR-ADMIN"].DepartmentId, byNumber["EMP-1001"].DepartmentId);
        Assert.Equal("مدير الشؤون الإدارية والمالية", byNumber["EMP-1001"].JobTitleAr);
        Assert.Equal(departments["SEC-BUDGET"].DepartmentId, byNumber["EMP-1103"].DepartmentId);
        Assert.Equal(departments["DEP-SYS-DEV"].DepartmentId, byNumber["EMP-2101"].DepartmentId);

        // Every seeded department has at least one employee, so no tree node opens empty.
        foreach (var department in departments.Values)
        {
            Assert.True(
                await db.Employees.AnyAsync(employee => employee.DepartmentId == department.DepartmentId),
                $"Department '{department.Code}' has no seeded employee.");
        }
    }

    [Fact]
    public async Task SeedAsync_IsIdempotent()
    {
        using var db = TestContextFactory.Create();
        await SeedPrerequisitesAsync(db);

        var seeder = new EmployeeSeeder();
        await seeder.SeedAsync(Context(db));
        var idsAfterFirstRun = await db.Employees
            .Select(employee => employee.EmployeeId)
            .OrderBy(id => id)
            .ToListAsync();

        await seeder.SeedAsync(Context(db));

        Assert.Equal(28, await db.Employees.CountAsync());
        Assert.Equal(
            idsAfterFirstRun,
            await db.Employees.Select(employee => employee.EmployeeId).OrderBy(id => id).ToListAsync());
    }

    [Fact]
    public async Task SeedAsync_DoesNotOverwriteExistingEmployeeNumber()
    {
        using var db = TestContextFactory.Create();
        await SeedPrerequisitesAsync(db);

        var department = await db.Departments.SingleAsync(d => d.Code == "DIR-ADMIN");
        var existing = Employee.Create(
            "موظف مخصص", department.DepartmentId, employeeNumber: "EMP-1001", jobTitleAr: "عنوان مخصص");
        Assert.True(existing.IsSuccess);
        db.Employees.Add(existing.Value);
        await db.SaveChangesAsync();

        await new EmployeeSeeder().SeedAsync(Context(db));

        Assert.Equal(28, await db.Employees.CountAsync());
        var employee = await db.Employees.SingleAsync(e => e.EmployeeNumber == "EMP-1001");
        Assert.Equal("موظف مخصص", employee.FullNameAr);
        Assert.Equal("عنوان مخصص", employee.JobTitleAr);
    }

    [Fact]
    public async Task SeedAsync_ThrowsWhenDepartmentIsMissing()
    {
        using var db = TestContextFactory.Create();
        await new DepartmentTypeSeeder().SeedAsync(Context(db));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => new EmployeeSeeder().SeedAsync(Context(db)));

        Assert.Contains("DepartmentSeeder", exception.Message);
        Assert.Empty(db.Employees);
    }

    [Fact]
    public void DemoPersonnel_IsGatedToDevelopmentAndStaging()
    {
        var seeder = new EmployeeSeeder();

        Assert.True(seeder.RunsIn(SeedingEnvironments.Development));
        Assert.True(seeder.RunsIn(SeedingEnvironments.Staging));
        Assert.False(seeder.RunsIn(SeedingEnvironments.Production));
        Assert.DoesNotContain(SeedingEnvironments.Production, seeder.Environments);
    }
}
