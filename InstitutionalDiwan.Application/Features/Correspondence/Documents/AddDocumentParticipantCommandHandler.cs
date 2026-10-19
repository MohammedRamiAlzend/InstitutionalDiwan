using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class AddDocumentParticipantCommandHandler
    : ICommandHandler<AddDocumentParticipantCommand, DocumentParticipantResponse>
{
    private const string ParticipantRolesLookup = "participant-roles";

    private readonly IDocumentRepository _documentRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IExternalOrganizationRepository _externalOrganizationRepository;
    private readonly ILookupRepository _lookupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddDocumentParticipantCommandHandler(
        IDocumentRepository documentRepository,
        IDepartmentRepository departmentRepository,
        IExternalOrganizationRepository externalOrganizationRepository,
        ILookupRepository lookupRepository,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _departmentRepository = departmentRepository;
        _externalOrganizationRepository = externalOrganizationRepository;
        _lookupRepository = lookupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DocumentParticipantResponse>> Handle(
        AddDocumentParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentParticipantResponse>(
                DomainErrors.Correspondence.DocumentNotFound);

        if (document.CurrentStatusId == DocumentStatusType.CanceledId)
            return Result.Failure<DocumentParticipantResponse>(
                DomainErrors.Correspondence.AlreadyCanceled);

        var targetResult = await ResolveTargetAsync(request, cancellationToken);
        if (targetResult.IsFailure)
            return Result.Failure<DocumentParticipantResponse>(targetResult.Error);

        var role = await _lookupRepository.GetByIdAsync(
            ParticipantRolesLookup, request.RoleId, cancellationToken);
        if (role is null || !role.IsActive)
            return Result.Failure<DocumentParticipantResponse>(
                DomainErrors.Correspondence.ParticipantRoleNotFound);

        var addResult = document.AddParticipant(
            request.RoleId,
            targetResult.Value,
            request.IsPrimary,
            request.Notes);

        if (addResult.IsFailure)
            return Result.Failure<DocumentParticipantResponse>(addResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentParticipantResponse.From(addResult.Value);
    }

    /// <summary>
    /// Enforces the participant-target invariant: exactly one of external organization, internal
    /// department, or person name, and — for the first two — that the referenced row exists and is
    /// active.
    /// </summary>
    private async Task<Result<ParticipantTarget>> ResolveTargetAsync(
        AddDocumentParticipantCommand request,
        CancellationToken cancellationToken)
    {
        var hasExternal = request.ExternalOrganizationId.HasValue;
        var hasInternal = request.InternalDepartmentId.HasValue;
        var hasPerson = !string.IsNullOrWhiteSpace(request.PersonName);

        var provided = (hasExternal ? 1 : 0) + (hasInternal ? 1 : 0) + (hasPerson ? 1 : 0);
        if (provided != 1)
            return Result.Failure<ParticipantTarget>(
                DomainErrors.Correspondence.ParticipantTargetInvalid);

        if (hasExternal)
        {
            var organization = await _externalOrganizationRepository.GetByIdAsync(
                request.ExternalOrganizationId!.Value, cancellationToken);

            if (organization is null || !organization.IsActive)
                return Result.Failure<ParticipantTarget>(
                    DomainErrors.Correspondence.ExternalOrgNotFound);

            return ParticipantTarget.External(organization.ExternalOrganizationId);
        }

        if (hasInternal)
        {
            var department = await _departmentRepository.GetByIdAsync(
                request.InternalDepartmentId!.Value, cancellationToken);

            if (department is null || !department.IsActive)
                return Result.Failure<ParticipantTarget>(
                    DomainErrors.Correspondence.ParticipantDepartmentNotFound);

            return ParticipantTarget.Internal(department.DepartmentId);
        }

        return ParticipantTarget.Person(request.PersonName);
    }
}
