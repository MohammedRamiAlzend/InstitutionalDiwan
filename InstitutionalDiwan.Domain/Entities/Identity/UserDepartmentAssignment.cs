using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Identity;

public sealed class UserDepartmentAssignment : Entity<Guid>
{
    public Guid AssignmentId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid UserId { get; private set; }
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid DepartmentId { get; private set; }
    public string AssignmentType { get; private set; } = "ADDITIONAL";
    public bool IncludeSubtree { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private UserDepartmentAssignment()
    {
    }

    internal static Result<UserDepartmentAssignment> Create(
        Guid userId,
        Guid departmentId,
        string assignmentType = "ADDITIONAL",
        bool includeSubtree = false,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var effectiveStart = startDate ?? DateTime.UtcNow;
        if (endDate.HasValue && endDate.Value < effectiveStart)
            return Result.Failure<UserDepartmentAssignment>(DomainErrors.Identity.InvalidDateRange);

        var assignment = new UserDepartmentAssignment
        {
            AssignmentId = Guid.NewGuid(),
            UserId = userId,
            DepartmentId = departmentId,
            AssignmentType = string.IsNullOrWhiteSpace(assignmentType) ? "ADDITIONAL" : assignmentType.Trim().ToUpperInvariant(),
            IncludeSubtree = includeSubtree,
            StartDate = effectiveStart,
            EndDate = endDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(assignment);
    }

    public Result EndAssignment(DateTime? endedAt = null)
    {
        IsActive = false;
        EndDate = endedAt ?? DateTime.UtcNow;
        return Result.Success();
    }
}
