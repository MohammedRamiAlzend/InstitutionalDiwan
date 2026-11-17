using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Ui.UiLabels;

/// <summary>Every stored UI label replacement, readable by any signed-in user so the SPA can boot.</summary>
public sealed record GetUiLabelOverridesQuery : IQuery<UiLabelOverridesResponse>;

internal sealed class GetUiLabelOverridesQueryHandler : IQueryHandler<GetUiLabelOverridesQuery, UiLabelOverridesResponse>
{
    private readonly IUiLabelOverrideRepository _labelOverrideRepository;

    public GetUiLabelOverridesQueryHandler(IUiLabelOverrideRepository labelOverrideRepository) =>
        _labelOverrideRepository = labelOverrideRepository;

    public async Task<Result<UiLabelOverridesResponse>> Handle(
        GetUiLabelOverridesQuery request,
        CancellationToken cancellationToken)
    {
        var overrides = await _labelOverrideRepository.ListAllAsync(cancellationToken);
        return UiLabelOverridesResponse.From(overrides);
    }
}