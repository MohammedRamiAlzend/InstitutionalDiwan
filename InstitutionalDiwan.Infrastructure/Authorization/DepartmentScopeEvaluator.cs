using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Infrastructure.Authorization;

/// <summary>
/// Resolves organizational scope from the caller's live persisted profile:
/// <list type="bullet">
///   <item><c>DEPARTMENT</c> — the primary department and any active department assignment.</item>
///   <item><c>DEPARTMENT_TREE</c> — the primary department's subtree, plus assignments
///   (assignments expand descendants when their <c>IncludeSubtree</c> flag is set).</item>
///   <item><c>INSTITUTION</c> / <c>SYSTEM</c> — every department.</item>
/// </list>
/// The active account, primary department, scope type, and assignments are re-read before any
/// access is granted; the token contributes only the caller's identity.
/// </summary>
public sealed class DepartmentScopeEvaluator : IDepartmentScopeEvaluator
{
    private readonly IUserContext _userContext;
    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IClock _clock;

    public DepartmentScopeEvaluator(
        IUserContext userContext,
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IClock clock)
    {
        _userContext = userContext;
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _clock = clock;
    }

    public async Task<bool> CanAccessDepartmentAsync(
        Guid departmentId,
        CancellationToken cancellationToken = default)
    {
        if (!_userContext.IsAuthenticated || departmentId == Guid.Empty)
            return false;

        var user = await _userRepository.GetByIdAsync(_userContext.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            return false;

        if (IsInstitutionWide(user.ScopeTypeId) || user.PrimaryDepartmentId == departmentId)
            return true;

        if (user.ScopeTypeId == UserScopeType.DepartmentTreeId &&
            await IsInsideSubtreeAsync(user.PrimaryDepartmentId, departmentId, cancellationToken))
        {
            return true;
        }

        var utcNow = _clock.UtcNow;
        foreach (var assignment in user.DepartmentAssignments.Where(
                     assignment => IsCurrentlyActive(assignment, utcNow)))
        {
            if (assignment.DepartmentId == departmentId)
                return true;

            if (assignment.IncludeSubtree &&
                await IsInsideSubtreeAsync(assignment.DepartmentId, departmentId, cancellationToken))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsInsideSubtreeAsync(
        Guid ancestorDepartmentId,
        Guid candidateDepartmentId,
        CancellationToken cancellationToken)
    {
        var descendants = await _departmentRepository.GetDescendantIdsAsync(
            ancestorDepartmentId, cancellationToken);

        return descendants.Contains(candidateDepartmentId);
    }

    private static bool IsCurrentlyActive(UserDepartmentAssignment assignment, DateTime utcNow) =>
        assignment.IsActive &&
        assignment.StartDate <= utcNow &&
        (assignment.EndDate is null || assignment.EndDate.Value > utcNow);

    private static bool IsInstitutionWide(int scopeTypeId) =>
        scopeTypeId is UserScopeType.InstitutionId or UserScopeType.SystemId;
}
