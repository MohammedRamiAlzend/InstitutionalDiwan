using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

internal sealed class GetDepartmentByIdQueryHandler
    : IQueryHandler<GetDepartmentByIdQuery, DepartmentResponse>
{
    private readonly IDepartmentRepository _departmentRepository;

    public GetDepartmentByIdQueryHandler(IDepartmentRepository departmentRepository) =>
        _departmentRepository = departmentRepository;

    public async Task<Result<DepartmentResponse>> Handle(
        GetDepartmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
            return Result.Failure<DepartmentResponse>(DomainErrors.Department.NotFound);

        return DepartmentResponse.From(department);
    }
}
