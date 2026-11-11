using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;

/// <summary>
/// Makes the given image the institution's logo, replacing any earlier one. Only PNG, JPEG, WebP and
/// plain SVG up to 1 MB are accepted; the kind is read from the bytes, not from the client.
/// </summary>
public sealed record SetInstitutionLogoCommand(byte[] Data) : ICommand<InstitutionSettingResponse>;

internal sealed class SetInstitutionLogoCommandHandler
    : ICommandHandler<SetInstitutionLogoCommand, InstitutionSettingResponse>
{
    private readonly IInstitutionSettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SetInstitutionLogoCommandHandler(IInstitutionSettingRepository settingRepository, IUnitOfWork unitOfWork)
    {
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InstitutionSettingResponse>> Handle(
        SetInstitutionLogoCommand request,
        CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return Result.Failure<InstitutionSettingResponse>(DomainErrors.InstitutionSetting.NotFound);

        var now = _unitOfWork.UtcNow;
        var existing = await _settingRepository.GetLogoAsync(cancellationToken);
        if (existing is null)
        {
            var created = InstitutionLogo.Create(setting.SettingId, request.Data, now);
            if (created.IsFailure)
                return Result.Failure<InstitutionSettingResponse>(created.Error);

            _settingRepository.AddLogo(created.Value);
        }
        else
        {
            var replaced = existing.Replace(request.Data, now);
            if (replaced.IsFailure)
                return Result.Failure<InstitutionSettingResponse>(replaced.Error);
        }

        // The setting row carries the change time: it is the logo's cache version and what the audit trail records.
        setting.MarkLogoChanged(now, now);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return InstitutionSettingResponse.From(setting);
    }
}
