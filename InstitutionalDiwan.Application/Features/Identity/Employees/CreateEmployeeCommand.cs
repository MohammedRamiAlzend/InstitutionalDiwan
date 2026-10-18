using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

/// <summary>Creates a new employee record attached to a department.</summary>
public sealed record CreateEmployeeCommand(
    string FullNameAr,
    Guid DepartmentId,
    string? EmployeeNumber = null,
    string? FullNameEn = null,
    string? JobTitleAr = null,
    string? Email = null,
    string? Phone = null) : ICommand<EmployeeResponse>;
