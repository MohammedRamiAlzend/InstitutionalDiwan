using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

internal sealed class ListEmployeesQueryHandler
    : IQueryHandler<ListEmployeesQuery, PagedResult<EmployeeResponse>>
{
    private readonly IEmployeeRepository _employeeRepository;

    public ListEmployeesQueryHandler(IEmployeeRepository employeeRepository) =>
        _employeeRepository = employeeRepository;

    public async Task<Result<PagedResult<EmployeeResponse>>> Handle(
        ListEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search);
        var employees = await _employeeRepository.ListPagedAsync(page, cancellationToken);

        return Result.Success(new PagedResult<EmployeeResponse>(
            employees.Items.Select(EmployeeResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            employees.TotalCount));
    }
}
