using InstitutionalDiwan.Domain.Entities.Organization;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the department-type lookup (Order 50, every environment) so departments always have a valid
/// <c>DepartmentTypeId</c> to reference. Ids are stable — see the constants on
/// <see cref="DepartmentType"/> — and the seeder is idempotent.
/// </summary>
public sealed class DepartmentTypeSeeder : ISeeder
{
    public int Order => 50;

    public IReadOnlyCollection<string> Environments { get; } = new[] { SeedingEnvironments.All };

    private static readonly (int Id, string Desc, string DescAr, int HierarchyLevelOrder)[] Catalog =
    {
        (DepartmentType.CentralDiwanId, "Central Diwan", "الديوان المركزي", 0),
        (DepartmentType.DirectorateId, "Directorate", "مديرية", 1),
        (DepartmentType.DepartmentId, "Department", "قسم", 2),
        (DepartmentType.SectionId, "Section", "شعبة", 3),
        (DepartmentType.UnitId, "Unit", "وحدة", 4),
    };

    public async Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
    {
        var db = context.Db;

        var existingIds = (await db.DepartmentTypes
                .Select(departmentType => departmentType.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var added = 0;
        foreach (var (id, desc, descAr, hierarchyLevelOrder) in Catalog)
        {
            if (existingIds.Contains(id))
                continue;

            var typeResult = DepartmentType.Create(id, desc, descAr, hierarchyLevelOrder);
            if (typeResult.IsFailure)
                throw new InvalidOperationException(
                    $"Invalid department type seed data '{desc}': {typeResult.Error.Code} - {typeResult.Error.Description}");

            db.DepartmentTypes.Add(typeResult.Value);
            added++;
        }

        if (added > 0)
            await db.SaveChangesAsync(cancellationToken);
    }
}
