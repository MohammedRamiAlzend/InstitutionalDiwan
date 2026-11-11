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
/// Appeal transitions are a later slice; reopening an archived file revokes a send-to-archive decision.
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
