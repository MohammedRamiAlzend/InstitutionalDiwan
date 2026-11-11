namespace InstitutionalDiwan.Domain.Entities.Decisions;

/// <summary>
/// What a decision does to the file. The set is fixed in code: a new effect needs a programmer,
/// while a new decision type is data an administrator defines.
/// </summary>
public static class DecisionEffect
{
    /// <summary>Open an ordinary movement toward <c>DestinationDepartmentId</c>.</summary>
    public const int ReferId = 1;

    /// <summary>Send the file along the archive route.</summary>
    public const int SendToArchiveId = 2;

    /// <summary>Join this file into another. Issuing it waits for the unification slice.</summary>
    public const int JoinId = 3;

    /// <summary>Record the decision and suggest nothing.</summary>
    public const int NoneId = 4;

    public static bool IsKnown(int effectId) =>
        effectId is ReferId or SendToArchiveId or JoinId or NoneId;
}

/// <summary>
/// Lifecycle of one decision. A decision that cannot be appealed is born <see cref="FinalId"/>.
/// Recording an appeal moves <see cref="IssuedId"/> to <see cref="AppealedId"/>. A rejected appeal
/// makes the decision <see cref="FinalId"/>; an accepted appeal makes it <see cref="RevokedId"/>,
/// which is not in force. Reopening an archived file also revokes a send-to-archive decision.
/// </summary>
public static class DecisionStatus
{
    public const int IssuedId = 1;
    public const int AppealedId = 2;
    public const int FinalId = 3;
    public const int RevokedId = 4;
    public const int CanceledId = 5;

    /// <summary>Issued, appealed, or final: no second decision may be issued while one of these stands.</summary>
    public static bool IsInForce(int statusId) =>
        statusId is IssuedId or AppealedId or FinalId;
}

/// <summary>
/// Ruling on one <see cref="DecisionAppeal"/>. The set is fixed in code, same as
/// <see cref="DecisionStatus"/>: 1 pending, 2 accepted, 3 rejected. An appeal is born pending.
/// Recording an outcome accepts only <see cref="AcceptedId"/> or <see cref="RejectedId"/>.
/// </summary>
public static class AppealOutcome
{
    /// <summary>Recorded, and waiting for a ruling.</summary>
    public const int PendingId = 1;

    /// <summary>Accepted. The decision is revoked and a new one may be issued.</summary>
    public const int AcceptedId = 2;

    /// <summary>Rejected. The decision becomes final.</summary>
    public const int RejectedId = 3;

    public static bool IsKnown(int outcomeId) =>
        outcomeId is PendingId or AcceptedId or RejectedId;

    /// <summary>The two rulings a user may record. Pending is not a ruling.</summary>
    public static bool IsRuling(int outcomeId) =>
        outcomeId is AcceptedId or RejectedId;
}
