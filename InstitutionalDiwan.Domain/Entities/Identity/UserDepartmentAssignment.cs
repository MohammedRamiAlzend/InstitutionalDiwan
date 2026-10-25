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
    // FK → AssignmentType.Id (InstitutionalDiwan.Domain.Entities.Identity.AssignmentType)
    public int AssignmentTypeId { get; private set; } = AssignmentType.AdditionalId;
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
        int assignmentTypeId = AssignmentType.AdditionalId,
        bool includeSubtree = false,
        DateTime? startDate = null,
        DateTime? endDate = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        var effectiveStart = startDate ?? now;
        if (endDate.HasValue && endDate.Value < effectiveStart)
            return Result.Failure<UserDepartmentAssignment>(DomainErrors.Identity.InvalidDateRange);

        var assignment = new UserDepartmentAssignment
        {
            AssignmentId = Guid.NewGuid(),
            UserId = userId,
            DepartmentId = departmentId,
            AssignmentTypeId = assignmentTypeId > 0 ? assignmentTypeId : AssignmentType.AdditionalId,
            IncludeSubtree = includeSubtree,
            StartDate = effectiveStart,
            EndDate = endDate,
            IsActive = true,
            CreatedAt = now
        };

        return Result.Success(assignment);
    }

    public Result EndAssignment(DateTime? endedAt = null, DateTime? utcNow = null)
    {
        IsActive = false;
        EndDate = endedAt ?? utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }
}
