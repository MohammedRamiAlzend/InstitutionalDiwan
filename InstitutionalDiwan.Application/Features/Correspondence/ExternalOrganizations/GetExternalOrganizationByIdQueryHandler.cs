using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

internal sealed class GetExternalOrganizationByIdQueryHandler
    : IQueryHandler<GetExternalOrganizationByIdQuery, ExternalOrganizationResponse>
{
    private readonly IExternalOrganizationRepository _externalOrganizationRepository;

    public GetExternalOrganizationByIdQueryHandler(
        IExternalOrganizationRepository externalOrganizationRepository) =>
        _externalOrganizationRepository = externalOrganizationRepository;

    public async Task<Result<ExternalOrganizationResponse>> Handle(
        GetExternalOrganizationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var organization = await _externalOrganizationRepository.GetByIdAsync(
            request.ExternalOrganizationId, cancellationToken);

        if (organization is null)
            return Result.Failure<ExternalOrganizationResponse>(
                DomainErrors.Correspondence.ExternalOrgNotFound);

        return ExternalOrganizationResponse.From(organization);
    }
}
