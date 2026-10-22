using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

internal sealed class ListExternalOrganizationsQueryHandler
    : IQueryHandler<ListExternalOrganizationsQuery, PagedResult<ExternalOrganizationResponse>>
{
    private readonly IExternalOrganizationRepository _externalOrganizationRepository;

    public ListExternalOrganizationsQueryHandler(
        IExternalOrganizationRepository externalOrganizationRepository) =>
        _externalOrganizationRepository = externalOrganizationRepository;

    public async Task<Result<PagedResult<ExternalOrganizationResponse>>> Handle(
        ListExternalOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search);
        var organizations = await _externalOrganizationRepository.ListPagedAsync(
            page, request.IncludeInactive, cancellationToken);

        return Result.Success(new PagedResult<ExternalOrganizationResponse>(
            organizations.Items.Select(ExternalOrganizationResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            organizations.TotalCount));
    }
}
