namespace InstitutionalDiwan.Domain.Entities.Identity;

public sealed class Role : AggregateRoot<Guid>
{
    private readonly List<RolePermission> _rolePermissions = new();

    public Guid RoleId
    {
        get => Id;
        private set => Id = value;
    }

    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private Role()
    {
    }

    public static Result<Role> Create(
        string? code,
        string? nameAr,
        string? nameEn = null,
        string? description = null,
        bool isSystemRole = false,
        DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<Role>(DomainErrors.Identity.RoleCodeEmpty);

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<Role>(DomainErrors.Identity.RoleNameEmpty);

        var role = new Role
        {
            RoleId = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            Description = description,
            IsSystemRole = isSystemRole,
            IsActive = true,
            CreatedAt = utcNow ?? DateTime.UtcNow
        };

        return Result.Success(role);
    }

    public Result GrantPermission(string? permissionCode, DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
            return Result.Failure(DomainErrors.Identity.PermissionCodeEmpty);

        var normalized = permissionCode.Trim().ToUpperInvariant();
        if (_rolePermissions.Any(p => p.PermissionCode == normalized))
            return Result.Failure(DomainErrors.Identity.PermissionAlreadyGranted);

        var rpResult = RolePermission.Create(RoleId, normalized, utcNow);
        if (rpResult.IsFailure)
            return rpResult;

        _rolePermissions.Add(rpResult.Value);
        return Result.Success();
    }

    public Result RevokePermission(string permissionCode)
    {
        var normalized = permissionCode.Trim().ToUpperInvariant();
        var existing = _rolePermissions.FirstOrDefault(p => p.PermissionCode == normalized);
        if (existing is not null)
            _rolePermissions.Remove(existing);

        return Result.Success();
    }

    public Result UpdateDetails(string? nameAr, string? nameEn = null, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure(DomainErrors.Identity.RoleNameEmpty);

        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (IsSystemRole)
            return Result.Failure(DomainErrors.Identity.SystemRoleProtected);

        IsActive = false;
        return Result.Success();
    }

    public Result Activate()
    {
        IsActive = true;
        return Result.Success();
    }
}
