using InstitutionalDiwan.Domain.Entities.Movements;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Read model of a document movement: where the document is, which step is current, and the
/// immutable trail of steps and events that got it there.
/// </summary>
public sealed record MovementResponse(
    Guid MovementId,
    Guid DocumentId,
    int MovementTypeId,
    Guid SourceDepartmentId,
    Guid DestinationDepartmentId,
    Guid? AssignedToUserId,
    int StatusId,
    int CurrentStepNo,
    string? Instructions,
    DateTime? DueDate,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? ClosedAt,
    IReadOnlyCollection<MovementStepResponse> Steps,
    IReadOnlyCollection<MovementEventResponse> Events)
{
    public static MovementResponse From(DocumentMovement movement) => new(
        movement.MovementId,
        movement.DocumentId,
        movement.MovementTypeId,
        movement.SourceDepartmentId,
        movement.DestinationDepartmentId,
        movement.AssignedToUserId,
        movement.StatusId,
        movement.CurrentStepNo,
        movement.Instructions,
        movement.DueDate,
        movement.CreatedByUserId,
        movement.CreatedAt,
        movement.ClosedAt,
        movement.Steps
            .OrderBy(step => step.StepNo)
            .Select(MovementStepResponse.From)
            .ToList(),
        movement.Events
            .OrderBy(movementEvent => movementEvent.OccurredAt)
            .Select(MovementEventResponse.From)
            .ToList());
}

/// <summary>One hop of a movement: the department pair and what happened to the step.</summary>
public sealed record MovementStepResponse(
    Guid StepId,
    int StepNo,
    Guid FromDepartmentId,
    Guid ToDepartmentId,
    Guid? AssignedToUserId,
    int StepStatusId,
    string? Instructions,
    string? OutcomeNotes,
    DateTime? SentAt,
    DateTime? ReceivedAt,
    Guid? ReceivedByUserId,
    Guid? RegistryNumberId,
    DateTime? CompletedAt,
    DateTime? DueDate)
{
    public static MovementStepResponse From(DocumentMovementStep step) => new(
        step.StepId,
        step.StepNo,
        step.FromDepartmentId,
        step.ToDepartmentId,
        step.AssignedToUserId,
        step.StepStatusId,
        step.Instructions,
        step.OutcomeNotes,
        step.SentAt,
        step.ReceivedAt,
        step.ReceivedByUserId,
        step.RegistryNumberId,
        step.CompletedAt,
        step.DueDate);
}

/// <summary>One entry of a movement's audit trail.</summary>
public sealed record MovementEventResponse(
    Guid EventId,
    int StepNo,
    int EventTypeId,
    Guid ActorUserId,
    string? Notes,
    DateTime OccurredAt)
{
    public static MovementEventResponse From(DocumentMovementEvent movementEvent) => new(
        movementEvent.EventId,
        movementEvent.StepNo,
        movementEvent.EventTypeId,
        movementEvent.ActorUserId,
        movementEvent.Notes,
        movementEvent.OccurredAt);
}
