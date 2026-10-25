namespace InstitutionalDiwan.Domain.Entities.Identity;

public sealed class Employee : AggregateRoot<Guid>
{
    public Guid EmployeeId
    {
        get => Id;
        private set => Id = value;
    }

    public string? EmployeeNumber { get; private set; }
    public string FullNameAr { get; private set; } = null!;
    public string? FullNameEn { get; private set; }
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid DepartmentId { get; private set; }
    public string? JobTitleAr { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private Employee()
    {
    }

    public static Result<Employee> Create(
        string? fullNameAr,
        Guid departmentId,
        string? employeeNumber = null,
        string? fullNameEn = null,
        string? jobTitleAr = null,
        string? email = null,
        string? phone = null,
        DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(fullNameAr))
            return Result.Failure<Employee>(DomainErrors.Identity.EmployeeNameEmpty);

        var employee = new Employee
        {
            EmployeeId = Guid.NewGuid(),
            FullNameAr = fullNameAr.Trim(),
            FullNameEn = string.IsNullOrWhiteSpace(fullNameEn) ? null : fullNameEn.Trim(),
            DepartmentId = departmentId,
            EmployeeNumber = string.IsNullOrWhiteSpace(employeeNumber) ? null : employeeNumber.Trim(),
            JobTitleAr = string.IsNullOrWhiteSpace(jobTitleAr) ? null : jobTitleAr.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            IsActive = true,
            CreatedAt = utcNow ?? DateTime.UtcNow
        };

        return Result.Success(employee);
    }

    public Result TransferDepartment(Guid newDepartmentId, string? newJobTitleAr = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        DepartmentId = newDepartmentId;
        if (!string.IsNullOrWhiteSpace(newJobTitleAr))
            JobTitleAr = newJobTitleAr.Trim();
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Update(
        string? fullNameAr,
        Guid departmentId,
        string? employeeNumber = null,
        string? fullNameEn = null,
        string? jobTitleAr = null,
        string? email = null,
        string? phone = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(fullNameAr))
            return Result.Failure(DomainErrors.Identity.EmployeeNameEmpty);

        FullNameAr = fullNameAr.Trim();
        FullNameEn = string.IsNullOrWhiteSpace(fullNameEn) ? null : fullNameEn.Trim();
        DepartmentId = departmentId;
        EmployeeNumber = string.IsNullOrWhiteSpace(employeeNumber) ? null : employeeNumber.Trim();
        JobTitleAr = string.IsNullOrWhiteSpace(jobTitleAr) ? null : jobTitleAr.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Deactivate(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        if (!IsActive) return Result.Success();
        IsActive = false;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Activate(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        if (IsActive) return Result.Success();
        IsActive = true;
        UpdatedAt = now;
        return Result.Success();
    }
}
