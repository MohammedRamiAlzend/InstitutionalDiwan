using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>
/// Starts a temporal department assignment for a user, extending their organizational scope.
/// When <paramref name="IncludeSubtree"/> is set the assignment also covers the department's
/// descendants. The user's primary department already grants access and cannot be re-assigned.
/// </summary>
public sealed record AssignUserDepartmentScopeCommand(
    Guid UserId,
    Guid DepartmentId,
    int AssignmentTypeId,
    bool IncludeSubtree = false,
    DateTime? StartDate = null,
    DateTime? EndDate = null) : ICommand<UserResponse>;
