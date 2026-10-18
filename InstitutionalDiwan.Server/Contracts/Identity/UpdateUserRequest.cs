namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Payload submitted to <c>PUT /api/users/{userId}</c>. The username is immutable.</summary>
public sealed record UpdateUserRequest(
    Guid PrimaryDepartmentId,
    Guid? EmployeeId,
    int ScopeTypeId,
    bool MustChangePassword);
