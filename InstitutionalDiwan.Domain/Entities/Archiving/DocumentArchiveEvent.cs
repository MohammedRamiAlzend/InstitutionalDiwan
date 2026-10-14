using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Archiving;

public sealed class DocumentArchiveEvent : Entity<Guid>
{
    public Guid ArchiveEventId
    {
        get => Id;
        private set => Id = value;
    }

    public Guid DocumentId { get; private set; }
    public string EventType { get; private set; } = null!;
    public string PreviousStatusCode { get; private set; } = null!;
    public string NewStatusCode { get; private set; } = null!;
    public Guid ActorUserId { get; private set; }
    public string? Reason { get; private set; }
    public string? ArchiveLocationCode { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private DocumentArchiveEvent()
    {
    }

    public static Result<DocumentArchiveEvent> Record(
        Guid documentId,
        string? eventType,
        string previousStatusCode,
        string newStatusCode,
        Guid actorUserId,
        string? reason = null,
        string? archiveLocationCode = null)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            return Result.Failure<DocumentArchiveEvent>(DomainErrors.Archiving.EventTypeInvalid);

        var normalized = eventType.Trim().ToUpperInvariant();
        if (normalized is not ("ARCHIVED" or "UNARCHIVED" or "RETAINED"))
            return Result.Failure<DocumentArchiveEvent>(DomainErrors.Archiving.EventTypeInvalid);

        var archiveEvent = new DocumentArchiveEvent
        {
            ArchiveEventId = Guid.NewGuid(),
            DocumentId = documentId,
            EventType = normalized,
            PreviousStatusCode = previousStatusCode,
            NewStatusCode = newStatusCode,
            ActorUserId = actorUserId,
            Reason = reason,
            ArchiveLocationCode = archiveLocationCode,
            OccurredAt = DateTime.UtcNow
        };

        return Result.Success(archiveEvent);
    }
}
