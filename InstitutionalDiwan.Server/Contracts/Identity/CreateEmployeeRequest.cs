namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Payload submitted to <c>POST /api/employees</c>.</summary>
public sealed record CreateEmployeeRequest(
    string FullNameAr,
    Guid DepartmentId,
    string? EmployeeNumber = null,
    string? FullNameEn = null,
    string? JobTitleAr = null,
    string? Email = null,
    string? Phone = null);
