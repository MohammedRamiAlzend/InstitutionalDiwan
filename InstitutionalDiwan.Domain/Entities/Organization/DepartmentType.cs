using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Organization;

public sealed class DepartmentType : Entity<string>
{
    public string Code
    {
        get => Id;
        private set => Id = value;
    }

    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public int HierarchyLevelOrder { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DepartmentType()
    {
    }

    public static Result<DepartmentType> Create(
        string? code,
        string? nameAr,
        int hierarchyLevelOrder,
        string? nameEn = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<DepartmentType>(DomainErrors.Department.TypeEmpty);

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<DepartmentType>(DomainErrors.Department.NameEmpty);

        var entity = new DepartmentType
        {
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            HierarchyLevelOrder = hierarchyLevelOrder,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(entity);
    }

    public Result Deactivate()
    {
        IsActive = false;
        return Result.Success();
    }
}
