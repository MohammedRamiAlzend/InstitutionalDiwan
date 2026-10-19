using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the singleton <see cref="InstitutionSetting"/> row (Order 95, every environment) so
/// <c>GET /api/institution-settings</c> always has something to return and numbering has a default
/// mode/policy. Runs after the numbering lookups (orders 68/90) it references, and before
/// <see cref="PermissionSeeder"/>.
///
/// The settings must designate a Central Diwan department, so the seeder first reuses an existing
/// department of type <see cref="DepartmentType.CentralDiwanId"/> or creates one as a root
/// department. The placeholder institution name is meant to be replaced through
/// <c>PUT /api/institution-settings</c>.
/// </summary>
public sealed class InstitutionSettingSeeder : ISeeder
{
    private const string CentralDiwanCode = "CENTRAL_DIWAN";

    public int Order => 95;

    public IReadOnlyCollection<string> Environments { get; } = new[] { SeedingEnvironments.All };

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var db = context.Db;

        // Singleton: seeded once, then only ever updated through the API.
        if (await db.InstitutionSettings.AnyAsync(cancellationToken))
            return;

        var centralDiwan = await EnsureCentralDiwanDepartmentAsync(db, cancellationToken);

        var settingResult = InstitutionSetting.Create(
            institutionNameAr: "المؤسسة",
            numberingModeId: NumberingMode.AutomaticId,
            numberingResetPolicyId: NumberingResetPolicy.YearlyId,
            centralDiwanDepartmentId: centralDiwan.DepartmentId);

        if (settingResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"Invalid institution settings seed data: {settingResult.Error.Code} - {settingResult.Error.Description}");
        }

        db.InstitutionSettings.Add(settingResult.Value);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Department> EnsureCentralDiwanDepartmentAsync(
        DiwanDbContext db,
        CancellationToken cancellationToken)
    {
        var existing = await db.Departments
            .FirstOrDefaultAsync(
                department => department.DepartmentTypeId == DepartmentType.CentralDiwanId,
                cancellationToken);

        if (existing is not null)
            return existing;

        var departmentResult = Department.Create(
            CentralDiwanCode,
            "الديوان المركزي",
            DepartmentType.CentralDiwanId,
            canSendExternal: true,
            canReceiveExternal: true,
            notes: "Root department seeded by InstitutionSettingSeeder.");

        if (departmentResult.IsFailure)
        {
            throw new InvalidOperationException(
                $"Invalid Central Diwan seed data: {departmentResult.Error.Code} - {departmentResult.Error.Description}");
        }

        db.Departments.Add(departmentResult.Value);
        await db.SaveChangesAsync(cancellationToken);

        return departmentResult.Value;
    }
}
