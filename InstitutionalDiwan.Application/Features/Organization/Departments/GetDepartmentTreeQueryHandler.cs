using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

internal sealed class GetDepartmentTreeQueryHandler
    : IQueryHandler<GetDepartmentTreeQuery, IReadOnlyCollection<DepartmentTreeNodeResponse>>
{
    private readonly IDepartmentRepository _departmentRepository;

    public GetDepartmentTreeQueryHandler(IDepartmentRepository departmentRepository) =>
        _departmentRepository = departmentRepository;

    public async Task<Result<IReadOnlyCollection<DepartmentTreeNodeResponse>>> Handle(
        GetDepartmentTreeQuery request,
        CancellationToken cancellationToken)
    {
        var departments = await _departmentRepository.ListAllAsync(cancellationToken);
        return Result.Success(DepartmentTreeNodeResponse.BuildTree(departments));
    }
}
