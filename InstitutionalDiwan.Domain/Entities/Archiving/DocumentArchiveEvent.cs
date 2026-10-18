using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Archiving;

/// <summary>
/// Audit record of an archival transition on a document. Status ids reference
/// <c>DocumentStatusType</c>; the event type and optional location reference their lookups.
/// </summary>
public sealed class DocumentArchiveEvent : Entity<Guid>
{
    public Guid ArchiveEventId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid DocumentId { get; private set; }
    // FK → ArchiveEventType.Id (InstitutionalDiwan.Domain.Entities.Archiving.ArchiveEventType)
    public int EventTypeId { get; private set; }
    // FK → DocumentStatusType.Id (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentStatusType)
    public int PreviousStatusId { get; private set; }
    // FK → DocumentStatusType.Id (InstitutionalDiwan.Domain.Entities.Correspondence.DocumentStatusType)
    public int NewStatusId { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid ActorUserId { get; private set; }
    public string? Reason { get; private set; }
    // FK → ArchiveLocation.Id (InstitutionalDiwan.Domain.Entities.Archiving.ArchiveLocation)
    public int? ArchiveLocationId { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private DocumentArchiveEvent()
    {
    }

    public static Result<DocumentArchiveEvent> Record(
        Guid documentId,
        int eventTypeId,
        int previousStatusId,
        int newStatusId,
        Guid actorUserId,
        string? reason = null,
        int? archiveLocationId = null)
    {
        if (eventTypeId is not (ArchiveEventType.ArchivedId or ArchiveEventType.UnarchivedId or ArchiveEventType.RetentionAppliedId))
            return Result.Failure<DocumentArchiveEvent>(DomainErrors.Archiving.EventTypeInvalid);

        if (previousStatusId <= 0 || newStatusId <= 0)
            return Result.Failure<DocumentArchiveEvent>(DomainErrors.Archiving.EventTypeInvalid);

        var ev = new DocumentArchiveEvent
        {
            ArchiveEventId = Guid.NewGuid(),
            DocumentId = documentId,
            EventTypeId = eventTypeId,
            PreviousStatusId = previousStatusId,
            NewStatusId = newStatusId,
            ActorUserId = actorUserId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            ArchiveLocationId = archiveLocationId,
            OccurredAt = DateTime.UtcNow
        };

        return Result.Success(ev);
    }
}
