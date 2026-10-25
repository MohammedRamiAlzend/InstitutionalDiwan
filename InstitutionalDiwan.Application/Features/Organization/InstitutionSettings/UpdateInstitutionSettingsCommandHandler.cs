using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;

internal sealed class UpdateInstitutionSettingsCommandHandler
    : ICommandHandler<UpdateInstitutionSettingsCommand, InstitutionSettingResponse>
{
    private const string NumberingModesLookup = "numbering-modes";
    private const string ResetPoliciesLookup = "numbering-reset-policies";

    private readonly IInstitutionSettingRepository _settingRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateInstitutionSettingsCommandHandler(
        IInstitutionSettingRepository settingRepository,
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        ILookupRepository lookupRepository,
        IUnitOfWork unitOfWork)
    {
        _settingRepository = settingRepository;
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _lookupRepository = lookupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InstitutionSettingResponse>> Handle(
        UpdateInstitutionSettingsCommand request,
        CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return Result.Failure<InstitutionSettingResponse>(DomainErrors.InstitutionSetting.NotFound);

        if (request.CentralDiwanDepartmentId is not { } centralDiwanDepartmentId)
            return Result.Failure<InstitutionSettingResponse>(DomainErrors.InstitutionSetting.CentralDiwanRequired);

        var departmentResult = await ValidateCentralDiwanAsync(centralDiwanDepartmentId, cancellationToken);
        if (departmentResult.IsFailure)
            return Result.Failure<InstitutionSettingResponse>(departmentResult.Error);

        var numberingModeResult = await ValidateLookupAsync(
            NumberingModesLookup,
            request.NumberingModeId,
            DomainErrors.InstitutionSetting.NumberingModeNotFound,
            DomainErrors.InstitutionSetting.NumberingModeInactive,
            cancellationToken);
        if (numberingModeResult.IsFailure)
            return Result.Failure<InstitutionSettingResponse>(numberingModeResult.Error);

        var resetPolicyResult = await ValidateLookupAsync(
            ResetPoliciesLookup,
            request.NumberingResetPolicyId,
            DomainErrors.InstitutionSetting.ResetPolicyNotFound,
            DomainErrors.InstitutionSetting.ResetPolicyInactive,
            cancellationToken);
        if (resetPolicyResult.IsFailure)
            return Result.Failure<InstitutionSettingResponse>(resetPolicyResult.Error);

        if (request.ResponsibleEmployeeId is { } employeeId &&
            await _employeeRepository.GetByIdAsync(employeeId, cancellationToken) is null)
        {
            return Result.Failure<InstitutionSettingResponse>(
                DomainErrors.InstitutionSetting.ResponsibleEmployeeNotFound);
        }

        var updateResult = setting.Update(
            request.InstitutionNameAr,
            request.NumberingModeId,
            request.NumberingResetPolicyId,
            centralDiwanDepartmentId,
            request.ResponsibleEmployeeId,
            request.InstitutionNameEn,
            request.LogoPath,
            request.DefaultTimezone,
            request.CentralDiwanTransferReason,
            utcNow: _unitOfWork.UtcNow);

        if (updateResult.IsFailure)
            return Result.Failure<InstitutionSettingResponse>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return InstitutionSettingResponse.From(setting);
    }

    /// <summary>
    /// The Central Diwan is the organization's root: the designated department must exist, be
    /// active, carry the <see cref="DepartmentType.CentralDiwanId"/> type, and have no parent.
    /// </summary>
    private async Task<Result> ValidateCentralDiwanAsync(
        Guid departmentId,
        CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken);
        if (department is null)
            return Result.Failure(DomainErrors.InstitutionSetting.CentralDiwanNotFound);

        if (department.DepartmentTypeId != DepartmentType.CentralDiwanId)
            return Result.Failure(DomainErrors.InstitutionSetting.CentralDiwanTypeInvalid);

        if (department.ParentDepartmentId is not null)
            return Result.Failure(DomainErrors.InstitutionSetting.CentralDiwanNotRoot);

        if (!department.IsActive)
            return Result.Failure(DomainErrors.InstitutionSetting.CentralDiwanInactive);

        return Result.Success();
    }

    private async Task<Result> ValidateLookupAsync(
        string lookupKey,
        int id,
        Error notFound,
        Error inactive,
        CancellationToken cancellationToken)
    {
        var entry = await _lookupRepository.GetByIdAsync(lookupKey, id, cancellationToken);
        if (entry is null)
            return Result.Failure(notFound);

        if (!entry.IsActive)
            return Result.Failure(inactive);

        return Result.Success();
    }
}
