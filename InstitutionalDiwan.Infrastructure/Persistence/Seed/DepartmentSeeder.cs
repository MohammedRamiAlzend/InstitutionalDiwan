using InstitutionalDiwan.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds a demo organization tree so the departments screen has a realistic hierarchy to display
/// (Order 96, Development and Staging only). The tree holds 15 departments across four levels: the
/// <c>CENTRAL_DIWAN</c> root owned by <see cref="InstitutionSettingSeeder"/>, four directorates,
/// seven departments, and three sections.
///
/// Runs after <see cref="InstitutionSettingSeeder"/> (Order 95) for the root and after
/// <see cref="DepartmentTypeSeeder"/> (Order 50) for the type ids it references. Idempotent by
/// <see cref="Department.Code"/>: existing rows are reused and never overwritten, and a missing row
/// is attached to its parent by code.
/// </summary>
public sealed class DepartmentSeeder : ISeeder
{
    public int Order => 96;

    public IReadOnlyCollection<string> Environments { get; } =
        new[] { SeedingEnvironments.Development, SeedingEnvironments.Staging };

    private sealed record DepartmentSeed(
        string Code,
        string NameAr,
        string NameEn,
        int DepartmentTypeId,
        string? ParentCode,
        bool CanSendExternal = false,
        bool CanReceiveExternal = false);

    /// <summary>
    /// Parent-first catalog: a null <c>ParentCode</c> attaches the row to the Central Diwan, and every
    /// other parent is listed before its children so the self-referencing foreign key always resolves.
    /// </summary>
    private static readonly DepartmentSeed[] Catalog =
    {
        // Directorates — level 1 under الديوان المركزي
        new("DIR-ADMIN", "مديرية الشؤون الإدارية والمالية", "Directorate of Administrative and Financial Affairs",
            DepartmentType.DirectorateId, null),
        new("DIR-IT", "مديرية تقنية المعلومات", "Directorate of Information Technology",
            DepartmentType.DirectorateId, null),
        new("DIR-LEGAL", "مديرية الشؤون القانونية", "Directorate of Legal Affairs",
            DepartmentType.DirectorateId, null),
        new("DIR-PR", "مديرية العلاقات العامة والإعلام", "Directorate of Public Relations and Media",
            DepartmentType.DirectorateId, null, CanSendExternal: true, CanReceiveExternal: true),

        // Departments — level 2
        new("DEP-HR", "قسم الموارد البشرية", "Human Resources Department",
            DepartmentType.DepartmentId, "DIR-ADMIN"),
        new("DEP-ACCOUNTING", "قسم المحاسبة", "Accounting Department",
            DepartmentType.DepartmentId, "DIR-ADMIN"),
        new("DEP-PROCUREMENT", "قسم المشتريات والمستودعات", "Procurement and Warehouses Department",
            DepartmentType.DepartmentId, "DIR-ADMIN"),
        new("DEP-SYS-DEV", "قسم تطوير الأنظمة", "Systems Development Department",
            DepartmentType.DepartmentId, "DIR-IT"),
        new("DEP-INFRA", "قسم البنية التحتية والشبكات", "Infrastructure and Networks Department",
            DepartmentType.DepartmentId, "DIR-IT"),
        new("DEP-CONTRACTS", "قسم العقود والمنازعات", "Contracts and Disputes Department",
            DepartmentType.DepartmentId, "DIR-LEGAL"),
        new("DEP-MEDIA", "قسم الإعلام والنشر", "Media and Publishing Department",
            DepartmentType.DepartmentId, "DIR-PR"),

        // Sections — level 3
        new("SEC-PAYROLL", "شعبة الأجور والتعويضات", "Payroll and Compensation Section",
            DepartmentType.SectionId, "DEP-HR"),
        new("SEC-PERSONNEL", "شعبة شؤون الموظفين", "Personnel Affairs Section",
            DepartmentType.SectionId, "DEP-HR"),
        new("SEC-BUDGET", "شعبة الموازنة", "Budget Section",
            DepartmentType.SectionId, "DEP-ACCOUNTING"),
    };

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var db = context.Db;

        var centralDiwan = await db.Departments
            .FirstOrDefaultAsync(
                department => department.DepartmentTypeId == DepartmentType.CentralDiwanId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Central Diwan department not found — InstitutionSettingSeeder (Order 95) must run before DepartmentSeeder.");

        var idByCode = (await db.Departments
                .Select(department => new { department.Code, department.DepartmentId })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                department => department.Code,
                department => department.DepartmentId,
                StringComparer.OrdinalIgnoreCase);

        foreach (var seed in Catalog)
        {
            if (idByCode.ContainsKey(seed.Code))
                continue;

            var parentId = seed.ParentCode is null
                ? centralDiwan.DepartmentId
                : idByCode[seed.ParentCode];

            var departmentResult = Department.Create(
                seed.Code,
                seed.NameAr,
                seed.DepartmentTypeId,
                parentDepartmentId: parentId,
                nameEn: seed.NameEn,
                canSendExternal: seed.CanSendExternal,
                canReceiveExternal: seed.CanReceiveExternal);

            if (departmentResult.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Invalid department seed data '{seed.Code}': {departmentResult.Error.Code} - {departmentResult.Error.Description}");
            }

            db.Departments.Add(departmentResult.Value);

            // Save level by level: EF cannot infer the insert order of the self-referencing FK.
            await db.SaveChangesAsync(cancellationToken);
            idByCode[seed.Code] = departmentResult.Value.DepartmentId;
        }
    }
}
