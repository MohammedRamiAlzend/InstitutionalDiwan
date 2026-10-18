using System;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Domain.Entities.Movements;

public sealed class DocumentMovementEvent : Entity<Guid>
{
    public Guid EventId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DocumentMovement.MovementId (InstitutionalDiwan.Domain.Entities.Movements.DocumentMovement)
    public Guid MovementId { get; private set; }
    public int StepNo { get; private set; }
    public string EventType { get; private set; } = null!;
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid ActorUserId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private DocumentMovementEvent()
    {
    }

    internal static DocumentMovementEvent Create(
        Guid movementId,
        int stepNo,
        string eventType,
        Guid actorUserId,
        string? notes = null)
    {
        return new DocumentMovementEvent
        {
            EventId = Guid.NewGuid(),
            MovementId = movementId,
            StepNo = stepNo,
            EventType = eventType.Trim().ToUpperInvariant(),
            ActorUserId = actorUserId,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            OccurredAt = DateTime.UtcNow
        };
    }
}
