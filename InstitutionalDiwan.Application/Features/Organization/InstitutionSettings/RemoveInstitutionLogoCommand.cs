using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;

/// <summary>Removes the uploaded logo so the default mark is shown again. Removing none is a no-op.</summary>
public sealed record RemoveInstitutionLogoCommand : ICommand<InstitutionSettingResponse>;

internal sealed class RemoveInstitutionLogoCommandHandler
    : ICommandHandler<RemoveInstitutionLogoCommand, InstitutionSettingResponse>
{
    private readonly IInstitutionSettingRepository _settingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveInstitutionLogoCommandHandler(IInstitutionSettingRepository settingRepository, IUnitOfWork unitOfWork)
    {
        _settingRepository = settingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<InstitutionSettingResponse>> Handle(
        RemoveInstitutionLogoCommand request,
        CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return Result.Failure<InstitutionSettingResponse>(DomainErrors.InstitutionSetting.NotFound);

        var existing = await _settingRepository.GetLogoAsync(cancellationToken);
        if (existing is not null)
        {
            _settingRepository.RemoveLogo(existing);
            setting.MarkLogoChanged(null, _unitOfWork.UtcNow);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return InstitutionSettingResponse.From(setting);
    }
}
