using InstitutionalDiwan.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the two diwans of the standard organization beside the Central Diwan root (Order 98,
/// <see cref="SeedingModes.Standard"/> only): <c>INDIVIDUAL_DIWAN</c> (ديوان الفرد) and
/// <c>INVESTIGATION_DIWAN</c> (ديوان التحقيق), both directorates directly under
/// <c>CENTRAL_DIWAN</c>. Idempotent by <see cref="Department.Code"/>.
/// </summary>
public sealed class StandardDepartmentSeeder : ISeeder
{
    public const string CentralDiwanCode = "CENTRAL_DIWAN";
    public const string IndividualDiwanCode = "INDIVIDUAL_DIWAN";
    public const string InvestigationDiwanCode = "INVESTIGATION_DIWAN";

    public int Order => 98;

    public IReadOnlyCollection<string> Environments { get; } = new[] { SeedingEnvironments.All };

    public IReadOnlyCollection<string> Modes { get; } = new[] { SeedingModes.Standard };

    private static readonly (string Code, string NameAr, string NameEn)[] Catalog =
    {
        (IndividualDiwanCode, "ديوان الفرد", "Individual Diwan"),
        (InvestigationDiwanCode, "ديوان التحقيق", "Investigation Diwan"),
    };

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var db = context.Db;

        var central = await db.Departments
            .FirstOrDefaultAsync(department => department.Code == CentralDiwanCode, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Department '{CentralDiwanCode}' not found — InstitutionSettingSeeder (Order 95) must run before StandardDepartmentSeeder.");

        foreach (var (code, nameAr, nameEn) in Catalog)
        {
            if (await db.Departments.AnyAsync(department => department.Code == code, cancellationToken))
                continue;

            var created = Department.Create(
                code,
                nameAr,
                DepartmentType.DirectorateId,
                central.DepartmentId,
                nameEn);

            if (created.IsFailure)
                throw new InvalidOperationException(
                    $"Invalid department seed data '{code}': {created.Error.Code} - {created.Error.Description}");

            db.Departments.Add(created.Value);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
