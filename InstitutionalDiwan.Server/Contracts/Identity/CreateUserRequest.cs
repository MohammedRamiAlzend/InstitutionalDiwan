namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>
/// Payload submitted to <c>POST /api/users</c>. <c>ScopeTypeId</c> defaults to 1
/// (<c>UserScopeType.Department</c>); roles are granted separately.
/// </summary>
public sealed record CreateUserRequest(
    string Username,
    string Password,
    Guid PrimaryDepartmentId,
    Guid? EmployeeId = null,
    int ScopeTypeId = 1,
    bool MustChangePassword = true);
