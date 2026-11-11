namespace InstitutionalDiwan.Server.Contracts.Decisions;

public sealed record CreateDecisionTypeRequest(
    string NameAr,
    bool IsAppealable,
    int EffectId,
    Guid? DestinationDepartmentId,
    IReadOnlyList<int> DocumentTypeIds);

public sealed record UpdateDecisionTypeRequest(
    string NameAr,
    bool IsAppealable,
    int EffectId,
    Guid? DestinationDepartmentId,
    IReadOnlyList<int> DocumentTypeIds);

public sealed record IssueDocumentDecisionRequest(
    Guid DecisionTypeId,
    string DecisionNumber,
    DateOnly DecisionDate,
    string? Summary,
    Guid? DocumentFileId);

public sealed record AttachDecisionFileRequest(Guid DocumentFileId);

public sealed record CancelDecisionRequest(string Reason);

public sealed record RecordDecisionAppealRequest(
    DateOnly AppealDate,
    Guid? AppellantDepartmentId,
    string? ReferenceNumber,
    string? Notes);

public sealed record RecordAppealOutcomeRequest(
    int OutcomeId,
    DateOnly OutcomeDate);
