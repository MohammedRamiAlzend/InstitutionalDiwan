namespace InstitutionalDiwan.Application.Abstractions.Authorization;

/// <summary>
/// Decides whether the caller may act on a given department. This is the second tier of the
/// three-tier access model: RBAC permissions say <em>what</em> a user may do, this evaluator says
/// <em>where</em> in the organization they may do it.
/// </summary>
public interface IDepartmentScopeEvaluator
{
    /// <summary>
    /// True when <paramref name="departmentId"/> is inside the caller's scope: their primary
    /// department, any active department assignment (expanding descendants when the assignment has
    /// <c>IncludeSubtree</c>), or every department when the caller's scope is institution-wide.
    /// </summary>
    Task<bool> CanAccessDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default);
}
