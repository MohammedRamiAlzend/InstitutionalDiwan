using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;

internal sealed class GetInstitutionSettingsQueryHandler
    : IQueryHandler<GetInstitutionSettingsQuery, InstitutionSettingResponse>
{
    private readonly IInstitutionSettingRepository _settingRepository;

    public GetInstitutionSettingsQueryHandler(IInstitutionSettingRepository settingRepository) =>
        _settingRepository = settingRepository;

    public async Task<Result<InstitutionSettingResponse>> Handle(
        GetInstitutionSettingsQuery request,
        CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return Result.Failure<InstitutionSettingResponse>(DomainErrors.InstitutionSetting.NotFound);

        return InstitutionSettingResponse.From(setting);
    }
}
