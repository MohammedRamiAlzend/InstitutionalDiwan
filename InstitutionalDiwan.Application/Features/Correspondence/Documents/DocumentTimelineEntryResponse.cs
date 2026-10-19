namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// One entry in a document's audit timeline. <see cref="EventKind"/> is <c>STATUS</c> for lifecycle
/// transitions, <c>PARTICIPANT</c> when a party was attached, and <c>NUMBER</c> for official-number
/// issue/cancel/replace events.
/// </summary>
public sealed record DocumentTimelineEntryResponse(
    string EventKind,
    DateTime OccurredAt,
    Guid ActorUserId,
    string? Description,
    Guid? ParticipantId = null,
    int? FromStatusId = null,
    int? ToStatusId = null,
    Guid? DocumentNumberId = null,
    int? NumberEventTypeId = null);
