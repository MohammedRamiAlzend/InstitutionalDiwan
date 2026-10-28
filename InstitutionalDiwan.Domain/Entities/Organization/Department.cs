using InstitutionalDiwan.Domain.Events;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Domain.Entities.Organization;

public sealed class Department : AggregateRoot<Guid>
{
    public Guid DepartmentId
    {
        get => Id;
        private set => Id = value;
    }

    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    // FK → DepartmentType.Id (InstitutionalDiwan.Domain.Entities.Organization.DepartmentType)
    public int DepartmentTypeId { get; private set; }
    // FK → Department.DepartmentId (self-reference, parent department)
    public Guid? ParentDepartmentId { get; private set; }
    // FK → Employee.EmployeeId (InstitutionalDiwan.Domain.Entities.Identity.Employee)
    public Guid? ManagerEmployeeId { get; private set; }
    public bool IsActive { get; private set; }
    public bool CanSendExternal { get; private set; }
    public bool CanReceiveExternal { get; private set; }
    public string? Notes { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Department()
    {
    }

    private Department(
        Guid departmentId,
        DepartmentCode code,
        string nameAr,
        string? nameEn,
        int departmentTypeId,
        Guid? parentDepartmentId,
        bool canSendExternal,
        bool canReceiveExternal,
        string? notes,
        DateTime createdAt)
        : base(departmentId)
    {
        DepartmentId = departmentId;
        Code = code.Value;
        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        DepartmentTypeId = departmentTypeId;
        ParentDepartmentId = parentDepartmentId;
        IsActive = true;
        CanSendExternal = canSendExternal;
        CanReceiveExternal = canReceiveExternal;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        CreatedAt = createdAt;

        RaiseDomainEvent(new DepartmentCreatedEvent(DepartmentId, Code, NameAr, ParentDepartmentId));
    }

    public static Result<Department> Create(
        string? code,
        string? nameAr,
        int departmentTypeId,
        Guid? parentDepartmentId = null,
        string? nameEn = null,
        bool canSendExternal = false,
        bool canReceiveExternal = false,
        string? notes = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        var codeResult = DepartmentCode.Create(code);
        if (codeResult.IsFailure)
            return Result.Failure<Department>(codeResult.Error);

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<Department>(DomainErrors.Department.NameEmpty);

        if (departmentTypeId <= 0)
            return Result.Failure<Department>(DomainErrors.Department.TypeEmpty);

        var department = new Department(
            Guid.NewGuid(),
            codeResult.Value,
            nameAr,
            nameEn,
            departmentTypeId,
            parentDepartmentId,
            canSendExternal,
            canReceiveExternal,
            notes,
            now);

        return Result.Success(department);
    }

    public Result Rename(string? nameAr, string? nameEn = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure(DomainErrors.Department.NameEmpty);

        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        UpdatedAt = now;
        return Result.Success();
    }

    public Result MoveToParent(Guid? newParentId, IReadOnlySet<Guid>? descendantDepartmentIds = null, bool newParentIsActive = true, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (newParentId.HasValue)
        {
            if (newParentId.Value == DepartmentId)
                return Result.Failure(DomainErrors.Department.SelfParent);

            if (!newParentIsActive)
                return Result.Failure(DomainErrors.Department.InactiveParent);

            if (descendantDepartmentIds is not null && descendantDepartmentIds.Contains(newParentId.Value))
                return Result.Failure(DomainErrors.Department.CyclicHierarchy);
        }

        var oldParentId = ParentDepartmentId;
        ParentDepartmentId = newParentId;
        UpdatedAt = now;

        RaiseDomainEvent(new DepartmentMovedEvent(DepartmentId, oldParentId, newParentId));
        return Result.Success();
    }

    public Result AssignManager(Guid? managerEmployeeId, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        ManagerEmployeeId = managerEmployeeId;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result UpdateNotes(string? notes, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        UpdatedAt = now;
        return Result.Success();
    }

    public Result UpdateExternalCorrespondencePolicy(bool canSendExternal, bool canReceiveExternal, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        CanSendExternal = canSendExternal;
        CanReceiveExternal = canReceiveExternal;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Deactivate(bool hasActiveChildren, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (hasActiveChildren)
            return Result.Failure(DomainErrors.Department.HasActiveChildren);

        if (!IsActive)
            return Result.Success();

        IsActive = false;
        UpdatedAt = now;
        RaiseDomainEvent(new DepartmentDeactivatedEvent(DepartmentId));
        return Result.Success();
    }

    public Result Activate(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        IsActive = true;
        UpdatedAt = now;
        return Result.Success();
    }
}
