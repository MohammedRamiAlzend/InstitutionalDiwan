namespace InstitutionalDiwan.Domain.Entities.Organization;

/// <summary>
/// Reference table classifying a department by its hierarchical level (Central Diwan, directorate,
/// department, section, unit). Replaces the former string <c>DepartmentTypeCode</c>:
/// <see cref="Department"/> now stores <see cref="Department.DepartmentTypeId"/>. Default rows are
/// seeded by <c>DepartmentTypeSeeder</c>; the stable ids below are the ones the seed uses.
/// <see cref="CentralDiwanId"/> is the root of the organization — the single department an
/// <see cref="InstitutionSetting"/> designates as the Central Diwan.
/// </summary>
public sealed class DepartmentType : LookupEntity
{
    public const int CentralDiwanId = 5;
    public const int DirectorateId = 1;
    public const int DepartmentId = 2;
    public const int SectionId = 3;
    public const int UnitId = 4;

    /// <summary>Ordering of this level within the organization hierarchy (lower is higher up).</summary>
    public int HierarchyLevelOrder { get; private set; }

    private DepartmentType()
    {
    }

    public static Result<DepartmentType> Create(int id, string? desc, string? descAr, int hierarchyLevelOrder)
    {
        if (id <= 0)
            return Result.Failure<DepartmentType>(DomainErrors.Department.TypeEmpty);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DepartmentType>(DomainErrors.Department.NameEmpty);

        var entity = new DepartmentType
        {
            Id = id,
            Desc = desc.Trim(),
            DescAr = descAr.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            HierarchyLevelOrder = hierarchyLevelOrder
        };

        return Result.Success(entity);
    }
}
