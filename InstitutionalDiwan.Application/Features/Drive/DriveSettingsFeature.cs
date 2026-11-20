using FluentValidation;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Reads the institution-wide Drive defaults. DRIVE_MANAGE only.</summary>
public sealed record GetDriveSettingsQuery : IQuery<DriveSettingsResponse>;

/// <summary>Updates the institution-wide Drive defaults (quota, max file size, allow-list). DRIVE_MANAGE only.</summary>
public sealed record UpdateDriveSettingsCommand(
    long DefaultQuotaBytes,
    long MaxFileSizeBytes,
    string? AllowedExtensionsCsv) : ICommand<DriveSettingsResponse>;

internal sealed class GetDriveSettingsQueryHandler : IQueryHandler<GetDriveSettingsQuery, DriveSettingsResponse>
{
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveAccessEvaluator _access;

    public GetDriveSettingsQueryHandler(IInstitutionSettingRepository settings, IDriveAccessEvaluator access)
    {
        _settings = settings;
        _access = access;
    }

    public async Task<Result<DriveSettingsResponse>> Handle(GetDriveSettingsQuery request, CancellationToken cancellationToken)
    {
        if (!_access.IsAdmin)
            return Result.Failure<DriveSettingsResponse>(DomainErrors.Drive.AccessDenied);

        var setting = await _settings.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return Result.Failure<DriveSettingsResponse>(DomainErrors.InstitutionSetting.NotFound);

        return Result.Success(DriveSettingsResponse.From(setting));
    }
}

internal sealed class UpdateDriveSettingsCommandHandler : ICommandHandler<UpdateDriveSettingsCommand, DriveSettingsResponse>
{
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateDriveSettingsCommandHandler(
        IInstitutionSettingRepository settings,
        IDriveAccessEvaluator access,
        IUnitOfWork unitOfWork)
    {
        _settings = settings;
        _access = access;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveSettingsResponse>> Handle(UpdateDriveSettingsCommand request, CancellationToken cancellationToken)
    {
        if (!_access.IsAdmin)
            return Result.Failure<DriveSettingsResponse>(DomainErrors.Drive.AccessDenied);

        var setting = await _settings.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return Result.Failure<DriveSettingsResponse>(DomainErrors.InstitutionSetting.NotFound);

        var updated = setting.UpdateDriveSettings(
            request.DefaultQuotaBytes, request.MaxFileSizeBytes, request.AllowedExtensionsCsv, _unitOfWork.UtcNow);
        if (updated.IsFailure)
            return Result.Failure<DriveSettingsResponse>(updated.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DriveSettingsResponse.From(setting));
    }
}

public sealed class UpdateDriveSettingsCommandValidator : AbstractValidator<UpdateDriveSettingsCommand>
{
    public UpdateDriveSettingsCommandValidator()
    {
        RuleFor(command => command.DefaultQuotaBytes)
            .GreaterThan(0).WithErrorCode("Drive.QuotaInvalid");
        RuleFor(command => command.MaxFileSizeBytes)
            .GreaterThan(0).WithErrorCode("Drive.MaxFileSizeInvalid");
        RuleFor(command => command.AllowedExtensionsCsv)
            .Must(extensions => !string.IsNullOrWhiteSpace(extensions)).WithErrorCode("Drive.ExtensionsEmpty");
    }
}