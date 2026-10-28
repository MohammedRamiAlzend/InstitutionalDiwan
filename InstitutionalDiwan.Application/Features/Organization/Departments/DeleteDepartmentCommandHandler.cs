using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.Departments;

internal sealed class DeleteDepartmentCommandHandler : ICommandHandler<DeleteDepartmentCommand>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IInstitutionSettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IInstitutionSettingRepository settingRepository,
        IUnitOfWork unitOfWork)
    {
        _departmentRepository = departmentRepository;
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
        if (department is null)
            return Result.Failure(DomainErrors.Department.NotFound);

        var setting = await _settingRepository.GetSingletonAsync(cancellationToken);
        if (setting?.CentralDiwanDepartmentId == department.DepartmentId)
            return Result.Failure(DomainErrors.Department.CentralDiwanProtected);

        var hasActiveChildren = await _departmentRepository.HasActiveChildrenAsync(
            department.DepartmentId, cancellationToken);

        var deactivateResult = department.Deactivate(hasActiveChildren, _unitOfWork.UtcNow);
        if (deactivateResult.IsFailure)
            return Result.Failure(deactivateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
