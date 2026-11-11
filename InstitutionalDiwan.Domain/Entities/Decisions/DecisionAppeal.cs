namespace InstitutionalDiwan.Domain.Entities.Decisions;

/// <summary>
/// One appeal against a <see cref="DocumentDecision"/>. It is born
/// <see cref="AppealOutcome.PendingId"/>. The decision aggregate records it and its outcome,
/// because those change the decision's status. There is no deadline, and an appeal does not
/// move the document.
/// </summary>
public sealed class DecisionAppeal
{
    public const int MaxReferenceLength = 100;
    public const int MaxNotesLength = 2000;

    public Guid AppealId { get; private set; }
    public Guid DecisionId { get; private set; }
    public DateOnly AppealDate { get; private set; }
    public Guid AppellantDepartmentId { get; private set; }
    public string? ReferenceNumber { get; private set; }
    public string? Notes { get; private set; }
    public int OutcomeId { get; private set; }
    public DateOnly? OutcomeDate { get; private set; }
    public Guid RecordedByUserId { get; private set; }
    public DateTime RecordedAt { get; private set; }
    public Guid? OutcomeRecordedByUserId { get; private set; }
    public DateTime? OutcomeRecordedAt { get; private set; }

    private DecisionAppeal()
    {
    }

    internal static DecisionAppeal Create(
        Guid decisionId,
        DateOnly appealDate,
        Guid appellantDepartmentId,
        string? referenceNumber,
        string? notes,
        Guid recordedByUserId,
        DateTime utcNow) =>
        new()
        {
            AppealId = Guid.NewGuid(),
            DecisionId = decisionId,
            AppealDate = appealDate,
            AppellantDepartmentId = appellantDepartmentId,
            ReferenceNumber = referenceNumber,
            Notes = notes,
            OutcomeId = AppealOutcome.PendingId,
            RecordedByUserId = recordedByUserId,
            RecordedAt = utcNow,
        };

    internal void RecordOutcome(int outcomeId, DateOnly outcomeDate, Guid actorUserId, DateTime utcNow)
    {
        OutcomeId = outcomeId;
        OutcomeDate = outcomeDate;
        OutcomeRecordedByUserId = actorUserId;
        OutcomeRecordedAt = utcNow;
    }
}
