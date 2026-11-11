using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Decisions;

/// <summary>
/// A decision written on one document. It keeps the type's appealability, effect, and destination as
/// they were at issuance. At most one decision on a document is in force (issued, appealed, or
/// final). A decision that cannot be appealed is born final.
/// </summary>
public sealed class DocumentDecision : AggregateRoot<Guid>
{
    public const int MaxNumberLength = 100;
    public const int MaxSummaryLength = 2000;
    public const int MaxReasonLength = 1000;

    public Guid DecisionId
    {
        get => Id;
        private set => Id = value;
    }

    public Guid DocumentId { get; private set; }
    public Guid DecisionTypeId { get; private set; }
    public string DecisionNumber { get; private set; } = null!;
    public DateOnly DecisionDate { get; private set; }
    public int DecisionYear { get; private set; }
    public Guid IssuingDepartmentId { get; private set; }
    public Guid RegistryVisitId { get; private set; }
    public string? Summary { get; private set; }
    public int StatusId { get; private set; }
    public bool IsAppealable { get; private set; }
    public int EffectId { get; private set; }
    public Guid? DestinationDepartmentId { get; private set; }
    public Guid? DocumentFileId { get; private set; }
    public Guid RecordedByUserId { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public string? StatusReason { get; private set; }
    public DateTime? StatusChangedAt { get; private set; }

    private readonly List<DecisionAppeal> _appeals = new();

    public IReadOnlyCollection<DecisionAppeal> Appeals => _appeals;

    public bool IsInForce => DecisionStatus.IsInForce(StatusId);

    private DocumentDecision()
    {
    }

    public static Result<DocumentDecision> Issue(
        Guid documentId,
        int documentTypeId,
        DecisionType type,
        string? decisionNumber,
        DateOnly decisionDate,
        Guid issuingDepartmentId,
        Guid registryVisitId,
        string? summary,
        Guid? documentFileId,
        Guid recordedByUserId,
        bool activeDecisionExists,
        bool numberTaken,
        DateTime? utcNow = null)
    {
        if (!type.IsActive)
            return Result.Failure<DocumentDecision>(DomainErrors.Decisions.TypeInactive);

        if (type.EffectId == DecisionEffect.JoinId)
            return Result.Failure<DocumentDecision>(DomainErrors.Decisions.JoinTargetInvalid);

        if (!type.AllowsDocumentType(documentTypeId))
            return Result.Failure<DocumentDecision>(DomainErrors.Decisions.TypeNotAllowedForDocument);

        if (activeDecisionExists)
            return Result.Failure<DocumentDecision>(DomainErrors.Decisions.ActiveDecisionExists);

        if (string.IsNullOrWhiteSpace(decisionNumber))
            return Result.Failure<DocumentDecision>(DomainErrors.Decisions.DecisionNumberEmpty);

        var number = decisionNumber.Trim();
        if (number.Length > MaxNumberLength)
            return Result.Failure<DocumentDecision>(DomainErrors.Decisions.DecisionNumberTooLong);

        if (decisionDate.Year is < 1900 or > 2200)
            return Result.Failure<DocumentDecision>(DomainErrors.Decisions.DecisionDateInvalid);

        if (numberTaken)
            return Result.Failure<DocumentDecision>(DomainErrors.Decisions.DecisionNumberTaken);

        if (summary is { Length: > MaxSummaryLength })
            return Result.Failure<DocumentDecision>(DomainErrors.Decisions.SummaryTooLong);

        if (issuingDepartmentId == Guid.Empty || registryVisitId == Guid.Empty)
            return Result.Failure<DocumentDecision>(DomainErrors.Decisions.NotHoldingDepartment);

        var now = utcNow ?? DateTime.UtcNow;
        var decision = new DocumentDecision
        {
            DecisionId = Guid.NewGuid(),
            DocumentId = documentId,
            DecisionTypeId = type.DecisionTypeId,
            DecisionNumber = number,
            DecisionDate = decisionDate,
            DecisionYear = decisionDate.Year,
            IssuingDepartmentId = issuingDepartmentId,
            RegistryVisitId = registryVisitId,
            Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim(),
            StatusId = type.IsAppealable ? DecisionStatus.IssuedId : DecisionStatus.FinalId,
            IsAppealable = type.IsAppealable,
            EffectId = type.EffectId,
            DestinationDepartmentId = type.DestinationDepartmentId,
            DocumentFileId = documentFileId is null || documentFileId == Guid.Empty ? null : documentFileId,
            RecordedByUserId = recordedByUserId,
            RecordedAt = now,
        };

        decision.RaiseDomainEvent(new DecisionIssuedEvent(
            decision.DecisionId,
            decision.DocumentId,
            decision.IssuingDepartmentId,
            decision.EffectId,
            recordedByUserId));

        return Result.Success(decision);
    }

    /// <summary>
    /// Ends a send-to-archive decision because the file was reopened. The reason is the reopen reason.
    /// </summary>
    public Result Revoke(Guid actorUserId, string? reason, DateTime? utcNow = null)
    {
        if (!IsInForce)
            return Result.Failure(DomainErrors.Decisions.DecisionNotFound);

        if (EffectId != DecisionEffect.SendToArchiveId)
            return Result.Success();

        return ChangeStatus(DecisionStatus.RevokedId, actorUserId, reason, utcNow);
    }

    /// <summary>Voids a decision that was entered by mistake. The reason is required.</summary>
    public Result Cancel(Guid actorUserId, string? reason, DateTime? utcNow = null)
    {
        if (StatusId == DecisionStatus.CanceledId)
            return Result.Failure(DomainErrors.Decisions.DecisionNotFound);

        return ChangeStatus(DecisionStatus.CanceledId, actorUserId, reason, utcNow);
    }

    public Result AttachFile(Guid documentFileId, DateTime? utcNow = null)
    {
        if (StatusId == DecisionStatus.CanceledId)
            return Result.Failure(DomainErrors.Decisions.DecisionNotFound);

        if (documentFileId == Guid.Empty)
            return Result.Failure(DomainErrors.Decisions.DecisionFileInvalid);

        DocumentFileId = documentFileId;
        StatusChangedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Records an appeal. Allowed only when the snapshot is appealable and the decision is still
    /// issued. A second appeal while one is pending is refused. The decision becomes appealed and
    /// the document is left untouched.
    /// </summary>
    public Result<DecisionAppeal> RecordAppeal(
        DateOnly appealDate,
        Guid appellantDepartmentId,
        string? referenceNumber,
        string? notes,
        Guid recordedByUserId,
        DateTime? utcNow = null)
    {
        if (!IsAppealable)
            return Result.Failure<DecisionAppeal>(DomainErrors.Decisions.NotAppealable);

        if (StatusId == DecisionStatus.AppealedId ||
            _appeals.Any(appeal => appeal.OutcomeId == AppealOutcome.PendingId))
            return Result.Failure<DecisionAppeal>(DomainErrors.Decisions.AppealAlreadyPending);

        if (StatusId != DecisionStatus.IssuedId)
            return Result.Failure<DecisionAppeal>(DomainErrors.Decisions.NotAppealable);

        if (!IsPlausibleDate(appealDate))
            return Result.Failure<DecisionAppeal>(DomainErrors.Decisions.AppealDateInvalid);

        if (appellantDepartmentId == Guid.Empty)
            return Result.Failure<DecisionAppeal>(DomainErrors.Department.NotFound);

        var reference = string.IsNullOrWhiteSpace(referenceNumber) ? null : referenceNumber.Trim();
        if (reference is { Length: > DecisionAppeal.MaxReferenceLength })
            return Result.Failure<DecisionAppeal>(DomainErrors.Decisions.AppealReferenceTooLong);

        var trimmedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (trimmedNotes is { Length: > DecisionAppeal.MaxNotesLength })
            return Result.Failure<DecisionAppeal>(DomainErrors.Decisions.AppealNotesTooLong);

        var now = utcNow ?? DateTime.UtcNow;
        var appeal = DecisionAppeal.Create(
            DecisionId,
            appealDate,
            appellantDepartmentId,
            reference,
            trimmedNotes,
            recordedByUserId,
            now);
        _appeals.Add(appeal);
        StatusId = DecisionStatus.AppealedId;
        StatusChangedAt = now;
        RaiseDomainEvent(new DecisionAppealedEvent(
            DecisionId, appeal.AppealId, DocumentId, appellantDepartmentId, recordedByUserId));
        return Result.Success(appeal);
    }

    /// <summary>
    /// Records a ruling on a pending appeal. Accepted revokes the decision; rejected makes it final.
    /// </summary>
    public Result RecordAppealOutcome(
        Guid appealId,
        int outcomeId,
        DateOnly outcomeDate,
        Guid actorUserId,
        DateTime? utcNow = null)
    {
        var appeal = _appeals.FirstOrDefault(item => item.AppealId == appealId);
        if (appeal is null)
            return Result.Failure(DomainErrors.Decisions.AppealNotFound);

        if (StatusId != DecisionStatus.AppealedId || appeal.OutcomeId != AppealOutcome.PendingId)
            return Result.Failure(DomainErrors.Decisions.AppealOutcomeInvalid);

        if (!AppealOutcome.IsRuling(outcomeId))
            return Result.Failure(DomainErrors.Decisions.AppealOutcomeInvalid);

        if (!IsPlausibleDate(outcomeDate))
            return Result.Failure(DomainErrors.Decisions.AppealDateInvalid);

        var now = utcNow ?? DateTime.UtcNow;
        appeal.RecordOutcome(outcomeId, outcomeDate, actorUserId, now);
        StatusId = outcomeId == AppealOutcome.AcceptedId
            ? DecisionStatus.RevokedId
            : DecisionStatus.FinalId;
        StatusChangedAt = now;
        return Result.Success();
    }

    private static bool IsPlausibleDate(DateOnly date) => date.Year is >= 1900 and <= 2200;

    private Result ChangeStatus(int statusId, Guid actorUserId, string? reason, DateTime? utcNow)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Decisions.CancelReasonRequired);

        var trimmed = reason.Trim();
        if (trimmed.Length > MaxReasonLength)
            return Result.Failure(DomainErrors.Decisions.ReasonTooLong);

        var now = utcNow ?? DateTime.UtcNow;
        StatusId = statusId;
        StatusReason = trimmed;
        StatusChangedAt = now;
        RaiseDomainEvent(new DecisionRevokedEvent(DecisionId, DocumentId, statusId, actorUserId, trimmed));
        return Result.Success();
    }
}
