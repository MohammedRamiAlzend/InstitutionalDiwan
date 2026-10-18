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

    // FK → DocumentMovement.MovementId (InstitutionalDiwan.Domain.Entities.Movements.DocumentMovement)
    public Guid MovementId { get; private set; }
    public int StepNo { get; private set; }
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid FromDepartmentId { get; private set; }
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid ToDepartmentId { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid? AssignedToUserId { get; private set; }
    // FK → MovementStepStatus.Id (InstitutionalDiwan.Domain.Entities.Movements.MovementStepStatus)
    public int StepStatusId { get; private set; } = MovementStepStatus.PendingId;
    public string? Instructions { get; private set; }
    public string? OutcomeNotes { get; private set; }
    public DateTime? SentAt { get; private set; }
    public DateTime? ReceivedAt { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
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
            StepStatusId = MovementStepStatus.PendingId,
            Instructions = instructions,
            DueDate = dueDate
        };
    }

    internal void MarkSent()
    {
        StepStatusId = MovementStepStatus.SentId;
        SentAt = DateTime.UtcNow;
    }

    internal void MarkReceived(Guid receivedByUserId)
    {
        StepStatusId = MovementStepStatus.ReceivedId;
        ReceivedAt = DateTime.UtcNow;
        ReceivedByUserId = receivedByUserId;
    }

    internal void MarkCompleted(string? notes)
    {
        StepStatusId = MovementStepStatus.CompletedId;
        OutcomeNotes = notes;
        CompletedAt = DateTime.UtcNow;
    }

    internal void MarkRejected(string reason)
    {
        StepStatusId = MovementStepStatus.RejectedId;
        OutcomeNotes = reason;
        CompletedAt = DateTime.UtcNow;
    }

    internal void MarkReturned(string reason)
    {
        StepStatusId = MovementStepStatus.ReturnedId;
        OutcomeNotes = reason;
        CompletedAt = DateTime.UtcNow;
    }

    internal void MarkRedirected(string? notes)
    {
        StepStatusId = MovementStepStatus.RedirectedId;
        OutcomeNotes = notes;
        CompletedAt = DateTime.UtcNow;
    }
}
