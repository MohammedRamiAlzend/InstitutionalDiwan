using System;
using InstitutionalDiwan.Domain.Common;

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

    private DocumentNumberHistory()
    {
    }

    internal static DocumentNumberHistory Create(
        Guid documentNumberId,
        int eventTypeId,
        Guid actorUserId,
        string? notes = null)
    {
        return new DocumentNumberHistory
        {
            HistoryId = Guid.NewGuid(),
            DocumentNumberId = documentNumberId,
            EventTypeId = eventTypeId,
            ActorUserId = actorUserId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            OccurredAt = DateTime.UtcNow
        };
    }
}
