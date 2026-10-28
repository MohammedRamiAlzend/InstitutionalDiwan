namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>Summary of a user account for admin listings and detail views.</summary>
public sealed record UserResponse(
    Guid UserId,
    string Username,
    Guid? EmployeeId,
    Guid PrimaryDepartmentId,
    int ScopeTypeId,
    bool IsActive,
    bool MustChangePassword,
    DateTime? LastLoginAt,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<UserDepartmentAssignmentResponse> DepartmentAssignments);
