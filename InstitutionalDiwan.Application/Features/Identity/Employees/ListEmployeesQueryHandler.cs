using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

internal sealed class ListEmployeesQueryHandler
    : IQueryHandler<ListEmployeesQuery, PagedResult<EmployeeResponse>>
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IDepartmentRepository _departmentRepository;

    public ListEmployeesQueryHandler(
        IEmployeeRepository employeeRepository,
        IDepartmentRepository departmentRepository)
    {
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<Result<PagedResult<EmployeeResponse>>> Handle(
        ListEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search);
        var departmentIds = await ResolveDepartmentIdsAsync(request, cancellationToken);
        var employees = await _employeeRepository.ListPagedAsync(page, departmentIds, cancellationToken);

        return Result.Success(new PagedResult<EmployeeResponse>(
            employees.Items.Select(EmployeeResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            employees.TotalCount));
    }

    private async Task<IReadOnlyCollection<Guid>?> ResolveDepartmentIdsAsync(
        ListEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        if (request.DepartmentId is not { } departmentId)
            return null;

        if (!request.IncludeSubDepartments)
            return new[] { departmentId };

        var ids = new HashSet<Guid> { departmentId };
        ids.UnionWith(await _departmentRepository.GetDescendantIdsAsync(departmentId, cancellationToken));
        return ids;
    }
}
