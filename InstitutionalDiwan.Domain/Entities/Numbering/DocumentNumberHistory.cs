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
    public string EventType { get; private set; } = null!;
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid ActorUserId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private DocumentNumberHistory()
    {
    }

    internal static DocumentNumberHistory Create(
        Guid documentNumberId,
        string eventType,
        Guid actorUserId,
        string? notes = null)
    {
        return new DocumentNumberHistory
        {
            HistoryId = Guid.NewGuid(),
            DocumentNumberId = documentNumberId,
            EventType = eventType.Trim().ToUpperInvariant(),
            ActorUserId = actorUserId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            OccurredAt = DateTime.UtcNow
        };
    }
}
