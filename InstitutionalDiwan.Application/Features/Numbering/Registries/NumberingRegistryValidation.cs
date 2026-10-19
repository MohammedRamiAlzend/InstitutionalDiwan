using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>
/// Scope checks shared by registry create/update: the referenced department, document type, and
/// direction must exist, and the reset policy must be an active seeded lookup entry.
/// </summary>
internal static class NumberingRegistryValidation
{
    private const string ResetPoliciesLookup = "numbering-reset-policies";

    public static async Task<Result> ValidateScopeAsync(
        Guid? departmentId,
        int? documentTypeId,
        int? directionId,
        IDepartmentRepository departmentRepository,
        IDocumentTypeRepository documentTypeRepository,
        CancellationToken cancellationToken)
    {
        if (departmentId is { } id && id != Guid.Empty &&
            await departmentRepository.GetByIdAsync(id, cancellationToken) is null)
        {
            return Result.Failure(DomainErrors.Department.NotFound);
        }

        if (documentTypeId is > 0)
        {
            var documentType = await documentTypeRepository.GetByIdAsync(documentTypeId.Value, cancellationToken);
            if (documentType is null)
                return Result.Failure(DomainErrors.Correspondence.DocumentTypeNotFound);

            if (!documentType.IsActive)
                return Result.Failure(DomainErrors.Correspondence.DocumentTypeInactive);
        }

        if (directionId is > 0 &&
            directionId.Value is not (DocumentDirection.InternalId
                or DocumentDirection.IncomingId
                or DocumentDirection.OutgoingId))
        {
            return Result.Failure(DomainErrors.Correspondence.DirectionInvalid);
        }

        return Result.Success();
    }

    public static async Task<Result> ValidateResetPolicyAsync(
        int resetPolicyId,
        ILookupRepository lookupRepository,
        CancellationToken cancellationToken)
    {
        var policy = await lookupRepository.GetByIdAsync(
            ResetPoliciesLookup, resetPolicyId, cancellationToken);

        if (policy is null)
            return Result.Failure(DomainErrors.InstitutionSetting.ResetPolicyNotFound);

        if (!policy.IsActive)
            return Result.Failure(DomainErrors.InstitutionSetting.ResetPolicyInactive);

        return Result.Success();
    }

    public static int NormalizeResetPolicy(int resetPolicyId) =>
        resetPolicyId > 0 ? resetPolicyId : NumberingResetPolicy.YearlyId;
}
