using InstitutionalDiwan.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds demo personnel so the organization screen can show who belongs to each department
/// (Order 97, Development and Staging only). Every employee is attached to a department seeded by
/// <see cref="DepartmentSeeder"/> (Order 96); the directorates get a director, the departments a
/// head and a specialist, and the sections a head and a clerk.
///
/// Idempotent by <see cref="Employee.EmployeeNumber"/>: existing rows are skipped and never
/// overwritten.
/// </summary>
public sealed class EmployeeSeeder : ISeeder
{
    public int Order => 97;

    public IReadOnlyCollection<string> Environments { get; } =
        new[] { SeedingEnvironments.Development, SeedingEnvironments.Staging };

    private sealed record EmployeeSeed(
        string EmployeeNumber,
        string DepartmentCode,
        string FullNameAr,
        string FullNameEn,
        string JobTitleAr,
        string? Email = null,
        string? Phone = null);

    private static readonly EmployeeSeed[] Catalog =
    {
        // الديوان المركزي
        new("EMP-0001", "CENTRAL_DIWAN", "نهاد عبد الله الرفاعي", "Nihad Abdullah Al-Rifai",
            "أمين عام الديوان", "nihad.rifai@diwan.gov.sy", "+963900000001"),
        new("EMP-0002", "CENTRAL_DIWAN", "سارة مروان الكيلاني", "Sara Marwan Al-Kilani",
            "مدير مكتب الأمين العام"),

        // مديرية الشؤون الإدارية والمالية
        new("EMP-1001", "DIR-ADMIN", "سامر عبد الرحمن الخطيب", "Samer Abdulrahman Al-Khatib",
            "مدير الشؤون الإدارية والمالية", "samer.khatib@diwan.gov.sy", "+963900100001"),
        new("EMP-1002", "DIR-ADMIN", "ريم محمود الأحمد", "Reem Mahmoud Al-Ahmad",
            "سكرتير تنفيذي", "reem.ahmad@diwan.gov.sy"),
        new("EMP-1101", "DEP-ACCOUNTING", "هيثم فؤاد النعيمي", "Haitham Fouad Al-Naimi",
            "رئيس قسم المحاسبة", "haitham.naimi@diwan.gov.sy"),
        new("EMP-1102", "DEP-ACCOUNTING", "لمى سامي الحسن", "Lama Sami Al-Hassan",
            "محاسب", "lama.hassan@diwan.gov.sy"),
        new("EMP-1103", "SEC-BUDGET", "عمر نبيل القاسم", "Omar Nabil Al-Qasim",
            "رئيس شعبة الموازنة"),
        new("EMP-1104", "SEC-BUDGET", "نور خالد ديب", "Nour Khaled Deeb",
            "محلل موازنة"),
        new("EMP-1201", "DEP-HR", "غادة ماهر السيد", "Ghada Maher Al-Sayed",
            "رئيس قسم الموارد البشرية", "ghada.sayed@diwan.gov.sy"),
        new("EMP-1202", "DEP-HR", "أحمد جمال العلي", "Ahmad Jamal Al-Ali",
            "أخصائي موارد بشرية"),
        new("EMP-1203", "SEC-PAYROLL", "إيمان يوسف حمدان", "Iman Yousef Hamdan",
            "رئيس شعبة الأجور والتعويضات"),
        new("EMP-1204", "SEC-PAYROLL", "بشار عدنان مرعي", "Bashar Adnan Marai",
            "مسؤول رواتب"),
        new("EMP-1205", "SEC-PERSONNEL", "رنا وليد الشامي", "Rana Walid Al-Shami",
            "رئيس شعبة شؤون الموظفين"),
        new("EMP-1301", "DEP-PROCUREMENT", "فادي رامي زيدان", "Fadi Rami Zaidan",
            "رئيس قسم المشتريات والمستودعات"),
        new("EMP-1302", "DEP-PROCUREMENT", "سلمى ناصر الحلبي", "Salma Nasser Al-Halabi",
            "مسؤول مشتريات"),

        // مديرية تقنية المعلومات
        new("EMP-2001", "DIR-IT", "مازن سمير حداد", "Mazen Samir Haddad",
            "مدير تقنية المعلومات", "mazen.haddad@diwan.gov.sy", "+963900200001"),
        new("EMP-2101", "DEP-SYS-DEV", "كرم أنس درويش", "Karam Anas Darwish",
            "رئيس قسم تطوير الأنظمة", "karam.darwish@diwan.gov.sy"),
        new("EMP-2102", "DEP-SYS-DEV", "لينا مروان صالح", "Lina Marwan Saleh",
            "مطورة برمجيات", "lina.saleh@diwan.gov.sy"),
        new("EMP-2103", "DEP-SYS-DEV", "يزن عماد الحاج", "Yazan Emad Al-Haj",
            "مطور واجهات"),
        new("EMP-2201", "DEP-INFRA", "نزار جهاد الحسين", "Nizar Jihad Al-Hussein",
            "رئيس قسم البنية التحتية والشبكات"),
        new("EMP-2202", "DEP-INFRA", "رهف عصام بركات", "Rahaf Issam Barakat",
            "مهندسة شبكات"),
        new("EMP-2203", "DEP-INFRA", "وليد سعيد حمدي", "Walid Saeed Hamdi",
            "فني صيانة"),

        // مديرية الشؤون القانونية
        new("EMP-3001", "DIR-LEGAL", "خلدون ماهر العظمة", "Khaldoun Maher Al-Azma",
            "مدير الشؤون القانونية", "khaldoun.azma@diwan.gov.sy"),
        new("EMP-3101", "DEP-CONTRACTS", "سوسن توفيق الملاح", "Sawsan Tawfiq Al-Mallah",
            "رئيس قسم العقود والمنازعات"),
        new("EMP-3102", "DEP-CONTRACTS", "أيمن فادي شريف", "Ayman Fadi Sharif",
            "مستشار قانوني"),

        // مديرية العلاقات العامة والإعلام
        new("EMP-4001", "DIR-PR", "لما غسان الشعار", "Lama Ghassan Al-Shaar",
            "مدير العلاقات العامة والإعلام", "lama.shaar@diwan.gov.sy"),
        new("EMP-4101", "DEP-MEDIA", "طارق نضال العبد", "Tarek Nidal Al-Abd",
            "رئيس قسم الإعلام والنشر"),
        new("EMP-4102", "DEP-MEDIA", "هبة مأمون الجندي", "Hiba Mamoun Al-Jundi",
            "محررة محتوى", "hiba.jundi@diwan.gov.sy"),
    };

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var db = context.Db;

        var idByCode = (await db.Departments
                .Select(department => new { department.Code, department.DepartmentId })
                .ToListAsync(cancellationToken))
            .ToDictionary(
                department => department.Code,
                department => department.DepartmentId,
                StringComparer.OrdinalIgnoreCase);

        var existingNumbers = (await db.Employees
                .Where(employee => employee.EmployeeNumber != null)
                .Select(employee => employee.EmployeeNumber!)
                .ToListAsync(cancellationToken))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in Catalog)
        {
            if (existingNumbers.Contains(seed.EmployeeNumber))
                continue;

            if (!idByCode.TryGetValue(seed.DepartmentCode, out var departmentId))
            {
                throw new InvalidOperationException(
                    $"Department '{seed.DepartmentCode}' not found — DepartmentSeeder (Order 96) must run before EmployeeSeeder.");
            }

            var employeeResult = Employee.Create(
                seed.FullNameAr,
                departmentId,
                employeeNumber: seed.EmployeeNumber,
                fullNameEn: seed.FullNameEn,
                jobTitleAr: seed.JobTitleAr,
                email: seed.Email,
                phone: seed.Phone);

            if (employeeResult.IsFailure)
            {
                throw new InvalidOperationException(
                    $"Invalid employee seed data '{seed.EmployeeNumber}': {employeeResult.Error.Code} - {employeeResult.Error.Description}");
            }

            db.Employees.Add(employeeResult.Value);
            await db.SaveChangesAsync(cancellationToken);
            existingNumbers.Add(seed.EmployeeNumber);
        }
    }
}
