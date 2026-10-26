using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Identity;

public sealed class User : AggregateRoot<Guid>
{
    private readonly List<UserRole> _userRoles = new();
    private readonly List<UserDepartmentAssignment> _departmentAssignments = new();

    public Guid UserId
    {
        get => Id;
        private set => Id = value;
    }

    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    // FK → Employee.EmployeeId (InstitutionalDiwan.Domain.Entities.Identity.Employee)
    public Guid? EmployeeId { get; private set; }
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid PrimaryDepartmentId { get; private set; }
    // FK → UserScopeType.Id (InstitutionalDiwan.Domain.Entities.Identity.UserScopeType)
    public int ScopeTypeId { get; private set; } = UserScopeType.DepartmentId;
    public bool IsActive { get; private set; }
    public bool MustChangePassword { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public IReadOnlyCollection<UserDepartmentAssignment> DepartmentAssignments => _departmentAssignments.AsReadOnly();

    private User()
    {
    }

    public static Result<User> Create(
        string? username,
        string? passwordHash,
        Guid primaryDepartmentId,
        Guid? employeeId = null,
        int scopeTypeId = UserScopeType.DepartmentId,
        bool mustChangePassword = true,
        DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result.Failure<User>(DomainErrors.Identity.UsernameEmpty);

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result.Failure<User>(DomainErrors.Identity.PasswordHashEmpty);

        var user = new User
        {
            UserId = Guid.NewGuid(),
            Username = username.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            PrimaryDepartmentId = primaryDepartmentId,
            EmployeeId = employeeId,
            ScopeTypeId = scopeTypeId > 0 ? scopeTypeId : UserScopeType.DepartmentId,
            IsActive = true,
            MustChangePassword = mustChangePassword,
            CreatedAt = utcNow ?? DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserCreatedEvent(user.UserId, user.Username, user.PrimaryDepartmentId));
        return Result.Success(user);
    }

    public Result AssignRole(Guid roleId, Guid? assignedByUserId = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (_userRoles.Any(r => r.RoleId == roleId))
            return Result.Failure(DomainErrors.Identity.RoleAlreadyAssigned);

        var userRoleResult = UserRole.Create(UserId, roleId, assignedByUserId, utcNow);
        if (userRoleResult.IsFailure)
            return userRoleResult;

        _userRoles.Add(userRoleResult.Value);
        UpdatedAt = now;
        RaiseDomainEvent(new UserRoleAssignedEvent(UserId, roleId, assignedByUserId));
        return Result.Success();
    }

    public Result RevokeRole(Guid roleId, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var existing = _userRoles.FirstOrDefault(r => r.RoleId == roleId);
        if (existing is null)
            return Result.Failure(DomainErrors.Identity.RoleNotAssigned);

        _userRoles.Remove(existing);
        UpdatedAt = now;
        RaiseDomainEvent(new UserRoleRevokedEvent(UserId, roleId));
        return Result.Success();
    }

    public Result AssignDepartmentScope(
        Guid departmentId,
        int assignmentTypeId = AssignmentType.AdditionalId,
        bool includeSubtree = false,
        DateTime? startDate = null,
        DateTime? endDate = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (_departmentAssignments.Any(assignment =>
                assignment.IsActive && assignment.DepartmentId == departmentId))
        {
            return Result.Failure(DomainErrors.Identity.DepartmentAlreadyAssigned);
        }

        var assignmentResult = UserDepartmentAssignment.Create(
            UserId,
            departmentId,
            assignmentTypeId,
            includeSubtree,
            startDate,
            endDate,
            utcNow);

        if (assignmentResult.IsFailure)
            return assignmentResult;

        _departmentAssignments.Add(assignmentResult.Value);
        UpdatedAt = now;
        return Result.Success();
    }

    /// <summary>
    /// Ends a temporal department assignment. The row is kept (with its end date) so the access
    /// history stays auditable; only its <see cref="UserDepartmentAssignment.IsActive"/> flag drops.
    /// </summary>
    public Result EndDepartmentScope(Guid assignmentId, DateTime? endedAt = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var assignment = _departmentAssignments.FirstOrDefault(
            candidate => candidate.AssignmentId == assignmentId);

        if (assignment is null)
            return Result.Failure(DomainErrors.Identity.DepartmentAssignmentNotFound);

        if (!assignment.IsActive)
            return Result.Success();

        assignment.EndAssignment(endedAt, utcNow);
        UpdatedAt = now;
        return Result.Success();
    }

    public Result RecordLogin(DateTime? utcNow = null)
    {
        LastLoginAt = utcNow ?? DateTime.UtcNow;
        RaiseDomainEvent(new UserLoggedInEvent(UserId, Username));
        return Result.Success();
    }

    public Result ChangePasswordHash(string? newPasswordHash, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Result.Failure(DomainErrors.Identity.PasswordHashEmpty);

        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result UpdateProfile(
        Guid primaryDepartmentId,
        Guid? employeeId,
        int scopeTypeId,
        bool mustChangePassword = true,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        PrimaryDepartmentId = primaryDepartmentId;
        EmployeeId = employeeId;
        ScopeTypeId = scopeTypeId > 0 ? scopeTypeId : UserScopeType.DepartmentId;
        MustChangePassword = mustChangePassword;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Deactivate(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        if (!IsActive) return Result.Success();
        IsActive = false;
        UpdatedAt = now;
        RaiseDomainEvent(new UserDeactivatedEvent(UserId));
        return Result.Success();
    }

    public Result Activate(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        if (IsActive) return Result.Success();
        IsActive = true;
        UpdatedAt = now;
        return Result.Success();
    }
}
