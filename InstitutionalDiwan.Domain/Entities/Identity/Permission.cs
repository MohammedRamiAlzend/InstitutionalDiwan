using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Identity;

public sealed class Permission : Entity<string>
{
    public string Code
    {
        get => Id;
        private set => Id = value;
    }

    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public string ModuleName { get; private set; } = null!;
    public string? Description { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Permission()
    {
    }

    public static Result<Permission> Create(
        string? code,
        string? nameAr,
        string? moduleName,
        string? nameEn = null,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<Permission>(DomainErrors.Identity.PermissionCodeEmpty);

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<Permission>(DomainErrors.Identity.RoleNameEmpty);

        if (string.IsNullOrWhiteSpace(moduleName))
            return Result.Failure<Permission>(DomainErrors.Identity.ModuleNameEmpty);

        var permission = new Permission
        {
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            ModuleName = moduleName.Trim().ToUpperInvariant(),
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(permission);
    }
}
