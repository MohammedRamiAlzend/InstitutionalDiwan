using InstitutionalDiwan.Domain.Entities.Identity;

namespace InstitutionalDiwan.Application.Features.Identity.Users;

/// <summary>One temporal department assignment held by a user.</summary>
public sealed record UserDepartmentAssignmentResponse(
    Guid AssignmentId,
    Guid DepartmentId,
    int AssignmentTypeId,
    bool IncludeSubtree,
    DateTime StartDate,
    DateTime? EndDate,
    bool IsActive)
{
    public static UserDepartmentAssignmentResponse From(UserDepartmentAssignment assignment) => new(
        assignment.AssignmentId,
        assignment.DepartmentId,
        assignment.AssignmentTypeId,
        assignment.IncludeSubtree,
        assignment.StartDate,
        assignment.EndDate,
        assignment.IsActive);
}
