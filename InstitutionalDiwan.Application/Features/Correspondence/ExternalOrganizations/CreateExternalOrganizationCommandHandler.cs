using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

internal sealed class CreateExternalOrganizationCommandHandler
    : ICommandHandler<CreateExternalOrganizationCommand, ExternalOrganizationResponse>
{
    private const string OrganizationTypesLookup = "organization-types";

    private readonly IExternalOrganizationRepository _externalOrganizationRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateExternalOrganizationCommandHandler(
        IExternalOrganizationRepository externalOrganizationRepository,
        ILookupRepository lookupRepository,
        IUnitOfWork unitOfWork)
    {
        _externalOrganizationRepository = externalOrganizationRepository;
        _lookupRepository = lookupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ExternalOrganizationResponse>> Handle(
        CreateExternalOrganizationCommand request,
        CancellationToken cancellationToken)
    {
        var code = (request.Code ?? string.Empty).Trim();
        if (code.Length == 0)
            return Result.Failure<ExternalOrganizationResponse>(
                DomainErrors.Correspondence.ExternalOrgCodeEmpty);

        if (code.Length > 50)
            return Result.Failure<ExternalOrganizationResponse>(
                DomainErrors.Correspondence.ExternalOrgCodeTooLong);

        if (await _externalOrganizationRepository.GetByCodeAsync(code, cancellationToken) is not null)
            return Result.Failure<ExternalOrganizationResponse>(
                DomainErrors.Correspondence.ExternalOrgCodeAlreadyExists);

        var organizationType = await _lookupRepository.GetByIdAsync(
            OrganizationTypesLookup, request.OrganizationTypeId, cancellationToken);
        if (organizationType is null)
            return Result.Failure<ExternalOrganizationResponse>(DomainErrors.Lookup.InvalidId);

        if (!organizationType.IsActive)
            return Result.Failure<ExternalOrganizationResponse>(DomainErrors.Lookup.EntryInactive(
                OrganizationTypesLookup, request.OrganizationTypeId));

        if (!ExternalOrganizationContact.IsValid(request.ContactInfo))
            return Result.Failure<ExternalOrganizationResponse>(
                DomainErrors.Correspondence.ExternalOrgContactInvalid);

        var createResult = ExternalOrganization.Create(
            code,
            request.NameAr,
            request.OrganizationTypeId,
            request.NameEn,
            request.ContactInfo);

        if (createResult.IsFailure)
            return Result.Failure<ExternalOrganizationResponse>(createResult.Error);

        _externalOrganizationRepository.Add(createResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ExternalOrganizationResponse.From(createResult.Value);
    }
}
