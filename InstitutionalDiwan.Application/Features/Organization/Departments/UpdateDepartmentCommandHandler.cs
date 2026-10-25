using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

internal sealed class UpdateDepartmentCommandHandler
    : ICommandHandler<UpdateDepartmentCommand, DepartmentResponse>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DepartmentResponse>> Handle(
        UpdateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
            return Result.Failure<DepartmentResponse>(DomainErrors.Department.NotFound);

        var renameResult = department.Rename(request.NameAr, request.NameEn, _unitOfWork.UtcNow);
        if (renameResult.IsFailure)
            return Result.Failure<DepartmentResponse>(renameResult.Error);

        if (department.ParentDepartmentId != request.ParentDepartmentId)
        {
            var newParentIsActive = true;
            IReadOnlySet<Guid>? descendantIds = null;

            if (request.ParentDepartmentId is { } parentDepartmentId)
            {
                var parent = await _departmentRepository.GetByIdAsync(parentDepartmentId, cancellationToken);
                if (parent is null)
                    return Result.Failure<DepartmentResponse>(DomainErrors.Department.ParentNotFound);

                newParentIsActive = parent.IsActive;
                descendantIds = await _departmentRepository.GetDescendantIdsAsync(
                    department.DepartmentId, cancellationToken);
            }

            var moveResult = department.MoveToParent(
                request.ParentDepartmentId, descendantIds, newParentIsActive, _unitOfWork.UtcNow);
            if (moveResult.IsFailure)
                return Result.Failure<DepartmentResponse>(moveResult.Error);
        }

        department.UpdateExternalCorrespondencePolicy(
            request.CanSendExternal, request.CanReceiveExternal, _unitOfWork.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DepartmentResponse.From(department);
    }
}
