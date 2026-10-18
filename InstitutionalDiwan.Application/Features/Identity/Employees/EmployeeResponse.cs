using InstitutionalDiwan.Domain.Entities.Identity;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

/// <summary>Read model returned by the employee endpoints.</summary>
public sealed record EmployeeResponse(
    Guid EmployeeId,
    string? EmployeeNumber,
    string FullNameAr,
    string? FullNameEn,
    Guid DepartmentId,
    string? JobTitleAr,
    string? Email,
    string? Phone,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static EmployeeResponse From(Employee employee) => new(
        employee.EmployeeId,
        employee.EmployeeNumber,
        employee.FullNameAr,
        employee.FullNameEn,
        employee.DepartmentId,
        employee.JobTitleAr,
        employee.Email,
        employee.Phone,
        employee.IsActive,
        employee.CreatedAt,
        employee.UpdatedAt);
}
