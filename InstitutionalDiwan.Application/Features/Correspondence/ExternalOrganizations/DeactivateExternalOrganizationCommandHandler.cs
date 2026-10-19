using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

internal sealed class DeactivateExternalOrganizationCommandHandler
    : ICommandHandler<DeactivateExternalOrganizationCommand, ExternalOrganizationResponse>
{
    private readonly IExternalOrganizationRepository _externalOrganizationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateExternalOrganizationCommandHandler(
        IExternalOrganizationRepository externalOrganizationRepository,
        IUnitOfWork unitOfWork)
    {
        _externalOrganizationRepository = externalOrganizationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ExternalOrganizationResponse>> Handle(
        DeactivateExternalOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        var organization = await _externalOrganizationRepository.GetByIdAsync(
            request.ExternalOrganizationId, cancellationToken);

        if (organization is null)
            return Result.Failure<ExternalOrganizationResponse>(
                DomainErrors.Correspondence.ExternalOrgNotFound);

        var deactivateResult = organization.Deactivate();
        if (deactivateResult.IsFailure)
            return Result.Failure<ExternalOrganizationResponse>(deactivateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ExternalOrganizationResponse.From(organization);
    }
}
