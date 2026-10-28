using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

internal sealed class UpdateDepartmentCommandHandler
    : ICommandHandler<UpdateDepartmentCommand, DepartmentResponse>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IInstitutionSettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        IInstitutionSettingRepository settingRepository,
        IUnitOfWork unitOfWork)
    {
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DepartmentResponse>> Handle(
        UpdateDepartmentCommand request,
        CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
            return Result.Failure<DepartmentResponse>(DomainErrors.Department.NotFound);

        if (request.ParentDepartmentId is not null &&
            department.ParentDepartmentId != request.ParentDepartmentId)
        {
            var setting = await _settingRepository.GetSingletonAsync(cancellationToken);
            if (setting?.CentralDiwanDepartmentId == department.DepartmentId)
                return Result.Failure<DepartmentResponse>(DomainErrors.Department.CentralDiwanProtected);
        }

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

        if (request.ManagerEmployeeId is { } managerEmployeeId)
        {
            var manager = await _employeeRepository.GetByIdAsync(managerEmployeeId, cancellationToken);
            if (manager is null)
                return Result.Failure<DepartmentResponse>(DomainErrors.Department.ManagerNotFound);
        }

        department.AssignManager(request.ManagerEmployeeId, _unitOfWork.UtcNow);
        department.UpdateNotes(request.Notes, _unitOfWork.UtcNow);
        department.UpdateExternalCorrespondencePolicy(
            request.CanSendExternal, request.CanReceiveExternal, _unitOfWork.UtcNow);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DepartmentResponse.From(department);
    }
}
