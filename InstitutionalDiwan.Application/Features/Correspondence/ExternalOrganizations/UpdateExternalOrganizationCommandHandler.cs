using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

internal sealed class UpdateExternalOrganizationCommandHandler
    : ICommandHandler<UpdateExternalOrganizationCommand, ExternalOrganizationResponse>
{
    private const string OrganizationTypesLookup = "organization-types";

    private readonly IExternalOrganizationRepository _externalOrganizationRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateExternalOrganizationCommandHandler(
        IExternalOrganizationRepository externalOrganizationRepository,
        ILookupRepository lookupRepository,
        IUnitOfWork unitOfWork)
    {
        _externalOrganizationRepository = externalOrganizationRepository;
        _lookupRepository = lookupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ExternalOrganizationResponse>> Handle(
        UpdateExternalOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        var organization = await _externalOrganizationRepository.GetByIdAsync(
            request.ExternalOrganizationId, cancellationToken);

        if (organization is null)
            return Result.Failure<ExternalOrganizationResponse>(
                DomainErrors.Correspondence.ExternalOrgNotFound);

        var organizationType = await _lookupRepository.GetByIdAsync(
            OrganizationTypesLookup, request.OrganizationTypeId, cancellationToken);
        if (organizationType is null || !organizationType.IsActive)
            return Result.Failure<ExternalOrganizationResponse>(DomainErrors.Lookup.InvalidId);

        if (!ExternalOrganizationContact.IsValid(request.ContactInfo))
            return Result.Failure<ExternalOrganizationResponse>(
                DomainErrors.Correspondence.ExternalOrgContactInvalid);

        var updateResult = organization.Update(
            request.NameAr,
            request.OrganizationTypeId,
            request.NameEn,
            request.ContactInfo);

        if (updateResult.IsFailure)
            return Result.Failure<ExternalOrganizationResponse>(updateResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ExternalOrganizationResponse.From(organization);
    }
}
