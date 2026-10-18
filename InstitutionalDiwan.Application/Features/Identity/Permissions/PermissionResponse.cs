using InstitutionalDiwan.Domain.Entities.Identity;

namespace InstitutionalDiwan.Application.Features.Identity.Permissions;

/// <summary>Read model for a single entry in the permission catalog.</summary>
public sealed record PermissionResponse(
    string Code,
    string NameAr,
    string? NameEn,
    string ModuleName,
    string? Description)
{
    public static PermissionResponse From(Permission permission) => new(
        permission.Code,
        permission.NameAr,
        permission.NameEn,
        permission.ModuleName,
        permission.Description);
}
