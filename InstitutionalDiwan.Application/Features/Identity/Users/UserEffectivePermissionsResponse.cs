namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>
/// Effective authorization state of a user. <see cref="Permissions"/> mirrors exactly what the
/// login token carries (<c>permission</c> claims), so UI gating and API enforcement agree.
/// </summary>
public sealed record UserEffectivePermissionsResponse(
    Guid UserId,
    string Username,
    int ScopeTypeId,
    Guid PrimaryDepartmentId,
    IReadOnlyCollection<string> RoleCodes,
    IReadOnlyCollection<string> Permissions,
    IReadOnlyCollection<UserDepartmentAssignmentResponse> DepartmentAssignments);
