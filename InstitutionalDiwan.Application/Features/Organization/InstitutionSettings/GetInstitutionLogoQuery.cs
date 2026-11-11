using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;

/// <summary>The bytes and media type of the uploaded logo.</summary>
public sealed record InstitutionLogoContent(byte[] Data, string ContentType, DateTime UpdatedAt);

/// <summary>Reads the uploaded institution logo, or fails with <c>InstitutionSetting.LogoNotFound</c>.</summary>
public sealed record GetInstitutionLogoQuery : IQuery<InstitutionLogoContent>;

internal sealed class GetInstitutionLogoQueryHandler : IQueryHandler<GetInstitutionLogoQuery, InstitutionLogoContent>
{
    private readonly IInstitutionSettingRepository _settingRepository;

    public GetInstitutionLogoQueryHandler(IInstitutionSettingRepository settingRepository) =>
        _settingRepository = settingRepository;

    public async Task<Result<InstitutionLogoContent>> Handle(
        GetInstitutionLogoQuery request,
        CancellationToken cancellationToken)
    {
        var logo = await _settingRepository.GetLogoAsync(cancellationToken);
        if (logo is null)
            return Result.Failure<InstitutionLogoContent>(DomainErrors.InstitutionSetting.LogoNotFound);

        return new InstitutionLogoContent(logo.Data, logo.ContentType, logo.UpdatedAt);
    }
}
