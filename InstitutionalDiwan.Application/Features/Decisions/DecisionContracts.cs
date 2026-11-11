using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Decisions;

public sealed record DecisionTypeResponse(
    Guid DecisionTypeId,
    string NameAr,
    bool IsAppealable,
    int EffectId,
    Guid? DestinationDepartmentId,
    bool IsActive,
    IReadOnlyList<int> DocumentTypeIds);

public sealed record DecisionAppealResponse(
    Guid AppealId,
    DateOnly AppealDate,
    Guid AppellantDepartmentId,
    string? ReferenceNumber,
    string? Notes,
    int OutcomeId,
    DateOnly? OutcomeDate,
    Guid RecordedByUserId,
    DateTime RecordedAt,
    Guid? OutcomeRecordedByUserId,
    DateTime? OutcomeRecordedAt);

public sealed record DocumentDecisionResponse(
    Guid DecisionId,
    Guid DocumentId,
    Guid DecisionTypeId,
    string TypeNameAr,
    string DecisionNumber,
    DateOnly DecisionDate,
    int DecisionYear,
    Guid IssuingDepartmentId,
    Guid RegistryVisitId,
    string? Summary,
    int StatusId,
    bool IsAppealable,
    int EffectId,
    Guid? DestinationDepartmentId,
    Guid? DocumentFileId,
    Guid RecordedByUserId,
    DateTime RecordedAt,
    string? StatusReason,
    IReadOnlyList<DecisionAppealResponse> Appeals);

public sealed record DecisionRegisterItemResponse(
    Guid DecisionId,
    Guid DocumentId,
    string DocumentTitle,
    string? OfficialNumberText,
    Guid DecisionTypeId,
    string TypeNameAr,
    string DecisionNumber,
    DateOnly DecisionDate,
    int DecisionYear,
    Guid IssuingDepartmentId,
    int StatusId,
    int EffectId,
    bool IsAppealable,
    Guid? DestinationDepartmentId);

/// <summary>
/// The one next step the screen should offer. <see cref="Kind"/> is <c>refer</c>,
/// <c>send-for-archive</c>, <c>archive</c>, or null when nothing is suggested.
/// </summary>
public sealed record NextActionResponse(
    string? Kind,
    Guid? DecisionId,
    Guid? DestinationDepartmentId,
    int? MovementTypeId);

public static class NextActionKind
{
    public const string Refer = "refer";
    public const string SendForArchive = "send-for-archive";
    public const string Archive = "archive";
}

public sealed record CreateDecisionTypeCommand(
    string NameAr,
    bool IsAppealable,
    int EffectId,
    Guid? DestinationDepartmentId,
    IReadOnlyList<int> DocumentTypeIds) : ICommand<DecisionTypeResponse>;

public sealed record UpdateDecisionTypeCommand(
    Guid DecisionTypeId,
    string NameAr,
    bool IsAppealable,
    int EffectId,
    Guid? DestinationDepartmentId,
    IReadOnlyList<int> DocumentTypeIds) : ICommand<DecisionTypeResponse>;

public sealed record ActivateDecisionTypeCommand(Guid DecisionTypeId) : ICommand<DecisionTypeResponse>;

public sealed record DeactivateDecisionTypeCommand(Guid DecisionTypeId) : ICommand<DecisionTypeResponse>;

public sealed record ListDecisionTypesQuery(int? DocumentTypeId, bool? IsActive) : IQuery<IReadOnlyList<DecisionTypeResponse>>;

public sealed record IssueDocumentDecisionCommand(
    Guid DocumentId,
    Guid UserId,
    Guid DecisionTypeId,
    string DecisionNumber,
    DateOnly DecisionDate,
    string? Summary,
    Guid? DocumentFileId) : ICommand<DocumentDecisionResponse>;

public sealed record AttachDecisionFileCommand(
    Guid DecisionId,
    Guid UserId,
    Guid DocumentFileId) : ICommand<DocumentDecisionResponse>;

public sealed record CancelDecisionCommand(
    Guid DecisionId,
    Guid UserId,
    string Reason) : ICommand<DocumentDecisionResponse>;

public sealed record RecordDecisionAppealCommand(
    Guid DecisionId,
    Guid UserId,
    DateOnly AppealDate,
    Guid? AppellantDepartmentId,
    string? ReferenceNumber,
    string? Notes) : ICommand<DocumentDecisionResponse>;

public sealed record RecordAppealOutcomeCommand(
    Guid DecisionId,
    Guid AppealId,
    Guid UserId,
    int OutcomeId,
    DateOnly OutcomeDate) : ICommand<DocumentDecisionResponse>;

public sealed record ListDocumentDecisionsQuery(Guid DocumentId) : IQuery<IReadOnlyList<DocumentDecisionResponse>>;

public sealed record GetDocumentNextActionQuery(Guid DocumentId) : IQuery<NextActionResponse>;

public sealed record ListDecisionsQuery(
    int Page,
    int PageSize,
    string? Search,
    string? SortBy,
    bool SortDescending,
    Guid? DecisionTypeId,
    int? StatusId,
    Guid? IssuingDepartmentId,
    int? Year) : IQuery<PagedResult<DecisionRegisterItemResponse>>;
