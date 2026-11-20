namespace InstitutionalDiwan.Domain.Entities.Drive;

/// <summary>
/// An explicit permission grant on a Drive item (Module 11) for a user or a department.
/// Effective access is computed by walking ancestors up to the space root; only explicit
/// grants are stored, and a node with broken inheritance ignores grants above it.
/// </summary>
public sealed class DrivePermission : Entity<Guid>
{
    public Guid PermissionId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DriveItem.ItemId
    public Guid ItemId { get; private set; }
    public int PrincipalTypeId { get; private set; }
    /// <summary>User.UserId or Department.DepartmentId depending on PrincipalTypeId.</summary>
    public Guid PrincipalId { get; private set; }
    public int RoleId { get; private set; }
    // FK → User.UserId
    public Guid GrantedByUserId { get; private set; }
    public DateTime GrantedAt { get; private set; }

    private DrivePermission()
    {
    }

    public static Result<DrivePermission> Create(
        Guid itemId,
        int principalTypeId,
        Guid principalId,
        int roleId,
        Guid grantedByUserId,
        DateTime? utcNow = null)
    {
        if (itemId == Guid.Empty)
            return Result.Failure<DrivePermission>(DomainErrors.Drive.ItemNotFound);
        if (!DrivePrincipalType.IsValid(principalTypeId))
            return Result.Failure<DrivePermission>(DomainErrors.Drive.PrincipalTypeInvalid);
        if (principalId == Guid.Empty)
            return Result.Failure<DrivePermission>(DomainErrors.Drive.PrincipalIdEmpty);
        if (!DriveRole.IsValid(roleId))
            return Result.Failure<DrivePermission>(DomainErrors.Drive.RoleInvalid);
        if (grantedByUserId == Guid.Empty)
            return Result.Failure<DrivePermission>(DomainErrors.Drive.PrincipalIdEmpty);

        var permission = new DrivePermission
        {
            PermissionId = Guid.NewGuid(),
            ItemId = itemId,
            PrincipalTypeId = principalTypeId,
            PrincipalId = principalId,
            RoleId = roleId,
            GrantedByUserId = grantedByUserId,
            GrantedAt = utcNow ?? DateTime.UtcNow
        };
        return Result.Success(permission);
    }

    public Result ChangeRole(int roleId)
    {
        if (!DriveRole.IsValid(roleId))
            return Result.Failure(DomainErrors.Drive.RoleInvalid);

        RoleId = roleId;
        return Result.Success();
    }
}