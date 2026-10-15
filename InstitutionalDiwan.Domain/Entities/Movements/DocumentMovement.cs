using System;
using System.Collections.Generic;
using System.Linq;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Movements;

public sealed class DocumentMovement : AggregateRoot<Guid>
{
    public const string StatusDraft = "DRAFT";
    public const string StatusSent = "SENT";
    public const string StatusReceived = "RECEIVED";
    public const string StatusInProgress = "IN_PROGRESS";
    public const string StatusCompleted = "COMPLETED";
    public const string StatusRejected = "REJECTED";
    public const string StatusReturned = "RETURNED";
    public const string StatusRedirected = "REDIRECTED";
    public const string StatusClosed = "CLOSED";
    public const string StatusCanceled = "CANCELED";

    private readonly List<DocumentMovementStep> _steps = new();
    private readonly List<DocumentMovementEvent> _events = new();

    public Guid MovementId
    {
        get => Id;
        private set => Id = value;
    }

    public Guid DocumentId { get; private set; }
    public string MovementType { get; private set; } = "REFERRAL";
    public Guid SourceDepartmentId { get; private set; }
    public Guid DestinationDepartmentId { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public string StatusCode { get; private set; } = StatusDraft;
    public int CurrentStepNo { get; private set; }
    public string? Instructions { get; private set; }
    public DateTime? DueDate { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public byte[]? RowVersion { get; private set; }

    public IReadOnlyCollection<DocumentMovementStep> Steps => _steps.AsReadOnly();
    public IReadOnlyCollection<DocumentMovementEvent> Events => _events.AsReadOnly();

    private DocumentMovement()
    {
    }

    public static Result<DocumentMovement> Create(
        Guid documentId,
        Guid sourceDepartmentId,
        Guid destinationDepartmentId,
        Guid createdByUserId,
        string movementType = "REFERRAL",
        Guid? assignedToUserId = null,
        string? instructions = null,
        DateTime? dueDate = null)
    {
        if (sourceDepartmentId == destinationDepartmentId && !assignedToUserId.HasValue)
            return Result.Failure<DocumentMovement>(DomainErrors.Movements.SameSourceAndDestination);

        var movement = new DocumentMovement
        {
            MovementId = Guid.NewGuid(),
            DocumentId = documentId,
            MovementType = string.IsNullOrWhiteSpace(movementType) ? "REFERRAL" : movementType.Trim().ToUpperInvariant(),
            SourceDepartmentId = sourceDepartmentId,
            DestinationDepartmentId = destinationDepartmentId,
            AssignedToUserId = assignedToUserId,
            StatusCode = StatusDraft,
            CurrentStepNo = 1,
            Instructions = instructions,
            DueDate = dueDate,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        var initialStep = DocumentMovementStep.Create(
            movement.MovementId,
            1,
            sourceDepartmentId,
            destinationDepartmentId,
            assignedToUserId,
            instructions,
            dueDate);

        movement._steps.Add(initialStep);
        movement.AppendEvent(1, "CREATED", createdByUserId, instructions);
        movement.RaiseDomainEvent(new MovementCreatedEvent(movement.MovementId, movement.DocumentId, sourceDepartmentId, destinationDepartmentId));

        return Result.Success(movement);
    }

    public Result Send(Guid actorUserId, string? notes = null)
    {
        if (StatusCode is not (StatusDraft or StatusRedirected))
            return Result.Failure(DomainErrors.Movements.InvalidStatusTransition);

        var step = GetCurrentStep();
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        step.MarkSent();
        StatusCode = StatusSent;
        AppendEvent(CurrentStepNo, "SENT", actorUserId, notes);
        RaiseDomainEvent(new MovementSentEvent(MovementId, DocumentId, CurrentStepNo, actorUserId));
        return Result.Success();
    }

    public Result ReceiveStep(Guid actorUserId, string? notes = null)
    {
        if (StatusCode != StatusSent)
            return Result.Failure(DomainErrors.Movements.InvalidStatusTransition);

        var step = GetCurrentStep();
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        step.MarkReceived(actorUserId);
        StatusCode = StatusReceived;
        AppendEvent(CurrentStepNo, "RECEIVED", actorUserId, notes);
        RaiseDomainEvent(new MovementReceivedEvent(MovementId, DocumentId, CurrentStepNo, actorUserId));
        return Result.Success();
    }

    public Result CompleteStep(Guid actorUserId, string? notes = null)
    {
        if (StatusCode is not (StatusReceived or StatusInProgress))
            return Result.Failure(DomainErrors.Movements.InvalidStatusTransition);

        var step = GetCurrentStep();
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        step.MarkCompleted(notes);
        StatusCode = StatusCompleted;
        AppendEvent(CurrentStepNo, "COMPLETED", actorUserId, notes);
        RaiseDomainEvent(new MovementCompletedEvent(MovementId, DocumentId, CurrentStepNo, actorUserId));
        return Result.Success();
    }

    public Result RejectStep(Guid actorUserId, string? reason)
    {
        if (StatusCode is not (StatusSent or StatusReceived or StatusInProgress))
            return Result.Failure(DomainErrors.Movements.InvalidStatusTransition);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Movements.ReasonRequired);

        var step = GetCurrentStep();
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        step.MarkRejected(reason.Trim());
        StatusCode = StatusRejected;
        AppendEvent(CurrentStepNo, "REJECTED", actorUserId, reason.Trim());
        RaiseDomainEvent(new MovementRejectedEvent(MovementId, DocumentId, CurrentStepNo, actorUserId, reason.Trim()));
        return Result.Success();
    }

    public Result ReturnToSender(Guid actorUserId, string? reason)
    {
        if (StatusCode is not (StatusReceived or StatusInProgress))
            return Result.Failure(DomainErrors.Movements.InvalidStatusTransition);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Movements.ReasonRequired);

        var step = GetCurrentStep();
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        step.MarkReturned(reason.Trim());
        StatusCode = StatusReturned;
        AppendEvent(CurrentStepNo, "RETURNED", actorUserId, reason.Trim());
        RaiseDomainEvent(new MovementReturnedEvent(MovementId, DocumentId, CurrentStepNo, actorUserId, reason.Trim()));
        return Result.Success();
    }

    public Result RedirectTo(
        Guid newDestinationDepartmentId,
        Guid actorUserId,
        Guid? assignedToUserId = null,
        string? instructions = null,
        DateTime? dueDate = null)
    {
        if (StatusCode is StatusClosed or StatusCanceled)
            return Result.Failure(DomainErrors.Movements.AlreadyTerminal);

        var currentStep = GetCurrentStep();
        currentStep?.MarkRedirected(instructions);

        var nextStepNo = CurrentStepNo + 1;
        var nextStep = DocumentMovementStep.Create(
            MovementId,
            nextStepNo,
            DestinationDepartmentId,
            newDestinationDepartmentId,
            assignedToUserId,
            instructions,
            dueDate);

        _steps.Add(nextStep);
        CurrentStepNo = nextStepNo;
        DestinationDepartmentId = newDestinationDepartmentId;
        AssignedToUserId = assignedToUserId;
        StatusCode = StatusSent;
        nextStep.MarkSent();

        AppendEvent(nextStepNo, "REDIRECTED", actorUserId, instructions);
        RaiseDomainEvent(new MovementRedirectedEvent(MovementId, DocumentId, nextStepNo, newDestinationDepartmentId, actorUserId));
        return Result.Success();
    }

    public Result Close(Guid actorUserId, string? notes = null)
    {
        if (StatusCode is StatusClosed or StatusCanceled)
            return Result.Failure(DomainErrors.Movements.AlreadyTerminal);

        StatusCode = StatusClosed;
        ClosedAt = DateTime.UtcNow;
        AppendEvent(CurrentStepNo, "CLOSED", actorUserId, notes);
        RaiseDomainEvent(new MovementClosedEvent(MovementId, DocumentId, actorUserId));
        return Result.Success();
    }

    public Result Cancel(Guid actorUserId, string? reason)
    {
        if (StatusCode is StatusClosed or StatusCanceled)
            return Result.Failure(DomainErrors.Movements.AlreadyTerminal);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Movements.ReasonRequired);

        StatusCode = StatusCanceled;
        ClosedAt = DateTime.UtcNow;
        AppendEvent(CurrentStepNo, "CANCELED", actorUserId, reason.Trim());
        RaiseDomainEvent(new MovementCancelledEvent(MovementId, DocumentId, actorUserId, reason.Trim()));
        return Result.Success();
    }

    private DocumentMovementStep? GetCurrentStep() =>
        _steps.FirstOrDefault(s => s.StepNo == CurrentStepNo);

    private void AppendEvent(int stepNo, string eventType, Guid actorUserId, string? notes)
    {
        _events.Add(DocumentMovementEvent.Create(MovementId, stepNo, eventType, actorUserId, notes));
    }
}
