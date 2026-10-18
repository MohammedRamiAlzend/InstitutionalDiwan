using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

internal sealed class ListDepartmentsQueryHandler
    : IQueryHandler<ListDepartmentsQuery, IReadOnlyCollection<DepartmentResponse>>
{
    private readonly IDepartmentRepository _departmentRepository;

    public ListDepartmentsQueryHandler(IDepartmentRepository departmentRepository) =>
        _departmentRepository = departmentRepository;

    public async Task<Result<IReadOnlyCollection<DepartmentResponse>>> Handle(
        ListDepartmentsQuery request,
        CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.ListAllAsync(cancellationToken);
        IReadOnlyCollection<DepartmentResponse> responses =
            departments.Select(DepartmentResponse.From).ToList();
        return Result.Success(responses);
    }
}
