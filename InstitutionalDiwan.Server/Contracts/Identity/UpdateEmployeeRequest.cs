namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Payload submitted to <c>PUT /api/employees/{employeeId}</c>. The employee id is immutable.</summary>
public sealed record UpdateEmployeeRequest(
    string FullNameAr,
    Guid DepartmentId,
    string? EmployeeNumber = null,
    string? FullNameEn = null,
    string? JobTitleAr = null,
    string? Email = null,
    string? Phone = null);
