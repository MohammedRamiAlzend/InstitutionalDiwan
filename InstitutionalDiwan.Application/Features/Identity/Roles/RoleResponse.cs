using InstitutionalDiwan.Domain.Entities.Identity;

namespace InstitutionalDiwan.Application.Features.Identity.Roles;

/// <summary>Read model returned by the role endpoints, including the role's granted permission codes.</summary>
public sealed record RoleResponse(
    Guid RoleId,
    string Code,
    string NameAr,
    string? NameEn,
    string? Description,
    bool IsSystemRole,
    bool IsActive,
    DateTime CreatedAt,
    IReadOnlyCollection<string> Permissions)
{
    public static RoleResponse From(Role role) => new(
        role.RoleId,
        role.Code,
        role.NameAr,
        role.NameEn,
        role.Description,
        role.IsSystemRole,
        role.IsActive,
        role.CreatedAt,
        role.RolePermissions
            .Select(permission => permission.PermissionCode)
            .OrderBy(code => code)
            .ToList());
}
