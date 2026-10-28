using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

/// <summary>
/// A page of employee records ordered by Arabic name, optionally filtered by a name or
/// employee-number search and by the department they belong to. <paramref name="IncludeSubDepartments"/>
/// widens the department filter to every descendant, which is what the organization tree needs.
/// </summary>
public sealed record ListEmployeesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    Guid? DepartmentId = null,
    bool IncludeSubDepartments = false,
    string? SortBy = null,
    bool SortDescending = false) : IQuery<PagedResult<EmployeeResponse>>;
