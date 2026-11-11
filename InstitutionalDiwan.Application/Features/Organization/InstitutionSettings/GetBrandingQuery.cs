using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;

/// <summary>The institution's public identity (name and logo version), readable before signing in.</summary>
public sealed record GetBrandingQuery : IQuery<BrandingResponse>;

internal sealed class GetBrandingQueryHandler : IQueryHandler<GetBrandingQuery, BrandingResponse>
{
    private readonly IInstitutionSettingRepository _settingRepository;

    public GetBrandingQueryHandler(IInstitutionSettingRepository settingRepository) =>
        _settingRepository = settingRepository;

    public async Task<Result<BrandingResponse>> Handle(GetBrandingQuery request, CancellationToken cancellationToken)
    {
        var setting = await _settingRepository.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return Result.Failure<BrandingResponse>(DomainErrors.InstitutionSetting.NotFound);

        return new BrandingResponse(setting.InstitutionNameAr, setting.InstitutionNameEn, setting.LogoUpdatedAt);
    }
}
