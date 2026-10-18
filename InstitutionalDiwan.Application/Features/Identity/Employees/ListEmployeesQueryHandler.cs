using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.Employees;

internal sealed class ListEmployeesQueryHandler
    : IQueryHandler<ListEmployeesQuery, IReadOnlyCollection<EmployeeResponse>>
{
    private readonly IEmployeeRepository _employeeRepository;

    public ListEmployeesQueryHandler(IEmployeeRepository employeeRepository) =>
        _employeeRepository = employeeRepository;

    public async Task<Result<IReadOnlyCollection<EmployeeResponse>>> Handle(
        ListEmployeesQuery request,
        CancellationToken cancellationToken)
    {
        var employees = await _employeeRepository.ListAllAsync(cancellationToken);
        IReadOnlyCollection<EmployeeResponse> responses = employees.Select(EmployeeResponse.From).ToList();
        return Result.Success(responses);
    }
}
