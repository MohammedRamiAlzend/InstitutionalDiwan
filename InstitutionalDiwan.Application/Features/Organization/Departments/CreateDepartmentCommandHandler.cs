using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

internal sealed class CreateDepartmentCommandHandler
    : ICommandHandler<CreateDepartmentCommand, DepartmentResponse>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDepartmentTypeRepository _departmentTypeRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IDepartmentTypeRepository departmentTypeRepository,
        IUnitOfWork unitOfWork)
    {
        _departmentRepository = departmentRepository;
        _departmentTypeRepository = departmentTypeRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DepartmentResponse>> Handle(
        CreateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        if (await _departmentRepository.GetByCodeAsync(request.Code, cancellationToken) is not null)
            return Result.Failure<DepartmentResponse>(DomainErrors.Department.CodeAlreadyExists);

        var departmentType = await _departmentTypeRepository.GetByIdAsync(
            request.DepartmentTypeId, cancellationToken);
        if (departmentType is null)
            return Result.Failure<DepartmentResponse>(DomainErrors.Department.TypeNotFound);

        if (!departmentType.IsActive)
            return Result.Failure<DepartmentResponse>(DomainErrors.Department.TypeInactive);

        if (request.ParentDepartmentId is { } parentDepartmentId)
        {
            var parent = await _departmentRepository.GetByIdAsync(parentDepartmentId, cancellationToken);
            if (parent is null)
                return Result.Failure<DepartmentResponse>(DomainErrors.Department.ParentNotFound);

            if (!parent.IsActive)
                return Result.Failure<DepartmentResponse>(DomainErrors.Department.InactiveParent);
        }

        var createResult = Department.Create(
            request.Code,
            request.NameAr,
            request.DepartmentTypeId,
            request.ParentDepartmentId,
            request.NameEn,
            request.CanSendExternal,
            request.CanReceiveExternal,
            request.Notes);

        if (createResult.IsFailure)
            return Result.Failure<DepartmentResponse>(createResult.Error);

        var department = createResult.Value;

        _departmentRepository.Add(department);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DepartmentResponse.From(department);
    }
}
