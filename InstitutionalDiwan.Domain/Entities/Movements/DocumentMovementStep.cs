using System;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Domain.Entities.Movements;

public sealed class DocumentMovementStep : Entity<Guid>
{
    public Guid StepId
    {
        get => Id;
        private set => Id = value;
    }

    public Guid MovementId { get; private set; }
    public int StepNo { get; private set; }
    public Guid FromDepartmentId { get; private set; }
    public Guid ToDepartmentId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public string StepStatus { get; private set; } = "PENDING";
    public string? Instructions { get; private set; }
    public string? OutcomeNotes { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    public Guid? ReceivedByUserId { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? DueDate { get; private set; }

    private DocumentMovementStep()
    {
    }

    internal static DocumentMovementStep Create(
        Guid movementId,
        int stepNo,
        Guid fromDepartmentId,
        Guid toDepartmentId,
        Guid? assignedToUserId = null,
        string? instructions = null,
        DateTime? dueDate = null)
    {
        return new DocumentMovementStep
        {
            StepId = Guid.NewGuid(),
            MovementId = movementId,
            StepNo = stepNo,
            FromDepartmentId = fromDepartmentId,
            ToDepartmentId = toDepartmentId,
            AssignedToUserId = assignedToUserId,
            StepStatus = "PENDING",
            Instructions = instructions,
            DueDate = dueDate
        };
    }

    internal void MarkSent()
    {
        StepStatus = "SENT";
        SentAt = DateTime.UtcNow;
    }

    internal void MarkReceived(Guid receivedByUserId)
    {
        StepStatus = "RECEIVED";
        ReceivedAt = DateTime.UtcNow;
        ReceivedByUserId = receivedByUserId;
    }

    internal void MarkCompleted(string? notes)
    {
        StepStatus = "COMPLETED";
        OutcomeNotes = notes;
        CompletedAt = DateTime.UtcNow;
    }

    internal void MarkRejected(string reason)
    {
        StepStatus = "REJECTED";
        OutcomeNotes = reason;
        CompletedAt = DateTime.UtcNow;
    }

    internal void MarkReturned(string reason)
    {
        StepStatus = "RETURNED";
        OutcomeNotes = reason;
        CompletedAt = DateTime.UtcNow;
    }

    internal void MarkRedirected(string? notes)
    {
        StepStatus = "REDIRECTED";
        OutcomeNotes = notes;
        CompletedAt = DateTime.UtcNow;
    }
}
