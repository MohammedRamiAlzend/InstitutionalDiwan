using InstitutionalDiwan.Domain.Entities.Decisions;

namespace InstitutionalDiwan.Application.Features.Decisions;

internal static class DecisionMapping
{
    public static DecisionTypeResponse ToResponse(DecisionType type) => new(
        type.DecisionTypeId,
        type.NameAr,
        type.IsAppealable,
        type.EffectId,
        type.DestinationDepartmentId,
        type.IsActive,
        type.DocumentTypes.Select(link => link.DocumentTypeId).OrderBy(id => id).ToList());

    public static DocumentDecisionResponse ToResponse(DocumentDecision decision, string typeNameAr) => new(
        decision.DecisionId,
        decision.DocumentId,
        decision.DecisionTypeId,
        typeNameAr,
        decision.DecisionNumber,
        decision.DecisionDate,
        decision.DecisionYear,
        decision.IssuingDepartmentId,
        decision.RegistryVisitId,
        decision.Summary,
        decision.StatusId,
        decision.IsAppealable,
        decision.EffectId,
        decision.DestinationDepartmentId,
        decision.DocumentFileId,
        decision.RecordedByUserId,
        decision.RecordedAt,
        decision.StatusReason,
        Appeals: []);
}
