using System;
using System.Collections.Generic;
using System.Linq;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
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
        bool mustChangePassword = true)
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
            CreatedAt = DateTime.UtcNow
        };

        user.RaiseDomainEvent(new UserCreatedEvent(user.UserId, user.Username, user.PrimaryDepartmentId));
        return Result.Success(user);
    }

    public Result AssignRole(Guid roleId, Guid? assignedByUserId = null)
    {
        if (_userRoles.Any(r => r.RoleId == roleId))
            return Result.Failure(DomainErrors.Identity.RoleAlreadyAssigned);

        var userRoleResult = UserRole.Create(UserId, roleId, assignedByUserId);
        if (userRoleResult.IsFailure)
            return userRoleResult;

        _userRoles.Add(userRoleResult.Value);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserRoleAssignedEvent(UserId, roleId, assignedByUserId));
        return Result.Success();
    }

    public Result RevokeRole(Guid roleId)
    {
        var existing = _userRoles.FirstOrDefault(r => r.RoleId == roleId);
        if (existing is null)
            return Result.Failure(DomainErrors.Identity.RoleNotAssigned);

        _userRoles.Remove(existing);
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserRoleRevokedEvent(UserId, roleId));
        return Result.Success();
    }

    public Result AssignDepartmentScope(
        Guid departmentId,
        int assignmentTypeId = AssignmentType.AdditionalId,
        bool includeSubtree = false,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var assignmentResult = UserDepartmentAssignment.Create(
            UserId,
            departmentId,
            assignmentTypeId,
            includeSubtree,
            startDate,
            endDate);

        if (assignmentResult.IsFailure)
            return assignmentResult;

        _departmentAssignments.Add(assignmentResult.Value);
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result ChangePasswordHash(string? newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Result.Failure(DomainErrors.Identity.PasswordHashEmpty);

        PasswordHash = newPasswordHash;
        MustChangePassword = false;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result UpdateProfile(
        Guid primaryDepartmentId,
        Guid? employeeId,
        int scopeTypeId,
        bool mustChangePassword = true)
    {
        PrimaryDepartmentId = primaryDepartmentId;
        EmployeeId = employeeId;
        ScopeTypeId = scopeTypeId > 0 ? scopeTypeId : UserScopeType.DepartmentId;
        MustChangePassword = mustChangePassword;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Deactivate()
    {
        if (!IsActive) return Result.Success();
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new UserDeactivatedEvent(UserId));
        return Result.Success();
    }

    public Result Activate()
    {
        if (IsActive) return Result.Success();
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
