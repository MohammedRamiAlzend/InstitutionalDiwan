using InstitutionalDiwan.Domain.Entities.Archiving;

namespace InstitutionalDiwan.Application.Features.Archiving;

/// <summary>One archive transition of a document, returned by its archive history endpoint.</summary>
public sealed record DocumentArchiveEventResponse(
    Guid ArchiveEventId,
    Guid DocumentId,
    int EventTypeId,
    int PreviousStatusId,
    int NewStatusId,
    Guid ActorUserId,
    string? Reason,
    int? ArchiveLocationId,
    DateTime OccurredAt)
{
    public static DocumentArchiveEventResponse From(DocumentArchiveEvent archiveEvent) => new(
        archiveEvent.ArchiveEventId,
        archiveEvent.DocumentId,
        archiveEvent.EventTypeId,
        archiveEvent.PreviousStatusId,
        archiveEvent.NewStatusId,
        archiveEvent.ActorUserId,
        archiveEvent.Reason,
        archiveEvent.ArchiveLocationId,
        archiveEvent.OccurredAt);
}
