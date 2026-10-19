namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>
/// Payload submitted to <c>POST /api/users/{userId}/department-assignments</c>. Omit
/// <c>AssignmentTypeId</c> to use ADDITIONAL; set <c>IncludeSubtree</c> to also cover the
/// department's descendants.
/// </summary>
public sealed record AssignUserDepartmentScopeRequest(
    Guid DepartmentId,
    int AssignmentTypeId = 0,
    bool IncludeSubtree = false,
    DateTime? StartDate = null,
    DateTime? EndDate = null);
