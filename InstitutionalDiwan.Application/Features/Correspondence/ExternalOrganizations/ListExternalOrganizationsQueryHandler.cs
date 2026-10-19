using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

internal sealed class ListExternalOrganizationsQueryHandler
    : IQueryHandler<ListExternalOrganizationsQuery, IReadOnlyCollection<ExternalOrganizationResponse>>
{
    private readonly IExternalOrganizationRepository _externalOrganizationRepository;

    public ListExternalOrganizationsQueryHandler(
        IExternalOrganizationRepository externalOrganizationRepository) =>
        _externalOrganizationRepository = externalOrganizationRepository;

    public async Task<Result<IReadOnlyCollection<ExternalOrganizationResponse>>> Handle(
        ListExternalOrganizationsQuery request,
        CancellationToken cancellationToken)
    {
        var organizations = await _externalOrganizationRepository.ListAllAsync(cancellationToken);

        IReadOnlyCollection<ExternalOrganizationResponse> response = organizations
            .Where(organization => request.IncludeInactive || organization.IsActive)
            .Select(ExternalOrganizationResponse.From)
            .ToList();

        return Result.Success(response);
    }
}
