using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

/// <summary>Updates an employee's details and department. The employee id is immutable.</summary>
public sealed record UpdateEmployeeCommand(
    Guid EmployeeId,
    string FullNameAr,
    Guid DepartmentId,
    string? EmployeeNumber = null,
    string? FullNameEn = null,
    string? JobTitleAr = null,
    string? Email = null,
    string? Phone = null) : ICommand<EmployeeResponse>;
