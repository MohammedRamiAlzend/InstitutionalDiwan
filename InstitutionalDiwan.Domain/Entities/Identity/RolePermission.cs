namespace InstitutionalDiwan.Domain.Entities.Identity;

public sealed class RolePermission
{
    // FK → Role.RoleId (InstitutionalDiwan.Domain.Entities.Identity.Role)
    public Guid RoleId { get; private set; }
    // FK → Permission.Code (InstitutionalDiwan.Domain.Entities.Identity.Permission)
    public string PermissionCode { get; private set; } = null!;
    public DateTime GrantedAt { get; private set; }

    private RolePermission()
    {
    }

    internal static Result<RolePermission> Create(Guid roleId, string? permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            return Result.Failure<RolePermission>(DomainErrors.Identity.PermissionCodeEmpty);

        var rp = new RolePermission
        {
            RoleId = roleId,
            PermissionCode = permissionCode.Trim().ToUpperInvariant(),
            GrantedAt = DateTime.UtcNow
        };

        return Result.Success(rp);
    }
}
