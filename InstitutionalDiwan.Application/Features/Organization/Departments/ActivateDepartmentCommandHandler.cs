using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

internal sealed class ActivateDepartmentCommandHandler
    : ICommandHandler<ActivateDepartmentCommand, DepartmentResponse>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DepartmentResponse>> Handle(
        ActivateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
            return Result.Failure<DepartmentResponse>(DomainErrors.Department.NotFound);

        var activateResult = department.Activate(_unitOfWork.UtcNow);
        if (activateResult.IsFailure)
            return Result.Failure<DepartmentResponse>(activateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DepartmentResponse.From(department);
    }
}
