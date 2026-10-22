using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

/// <summary>
/// A page of employee records ordered by Arabic name, optionally filtered by a name or
/// employee-number search.
/// </summary>
public sealed record ListEmployeesQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null) : IQuery<PagedResult<EmployeeResponse>>;
