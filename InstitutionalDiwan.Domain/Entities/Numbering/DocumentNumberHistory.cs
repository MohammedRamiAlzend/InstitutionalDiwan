namespace InstitutionalDiwan.Domain.Entities.Numbering;

public sealed class DocumentNumberHistory : Entity<Guid>
{
    public Guid HistoryId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DocumentNumber.DocumentNumberId (InstitutionalDiwan.Domain.Entities.Numbering.DocumentNumber)
    public Guid DocumentNumberId { get; private set; }
    // FK → DocumentNumberEventType.Id (InstitutionalDiwan.Domain.Entities.Numbering.DocumentNumberEventType)
    public int EventTypeId { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid ActorUserId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime OccurredAt { get; private set; }
    /// <summary>
    /// Date written in the department register for this event. Null for central register events,
    /// which are dated by <see cref="OccurredAt"/>.
    /// </summary>
    public DateTime? EffectiveDate { get; private set; }

    private DocumentNumberHistory()
    {
    }

    internal static DocumentNumberHistory Create(
        Guid documentNumberId,
        int eventTypeId,
        Guid actorUserId,
        string? notes = null,
        DateTime? effectiveDate = null,
        DateTime? utcNow = null)
    {
        return new DocumentNumberHistory
        {
            HistoryId = Guid.NewGuid(),
            DocumentNumberId = documentNumberId,
            EventTypeId = eventTypeId,
            ActorUserId = actorUserId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            OccurredAt = utcNow ?? DateTime.UtcNow,
            EffectiveDate = effectiveDate
        };
    }
}
