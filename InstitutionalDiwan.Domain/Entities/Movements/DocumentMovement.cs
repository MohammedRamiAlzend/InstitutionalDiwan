using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Movements;

public sealed class DocumentMovement : AggregateRoot<Guid>
{
    public const int StatusDraft = MovementStatus.DraftId;
    public const int StatusSent = MovementStatus.SentId;
    public const int StatusReceived = MovementStatus.ReceivedId;
    public const int StatusInProgress = MovementStatus.InProgressId;
    public const int StatusCompleted = MovementStatus.CompletedId;
    public const int StatusRejected = MovementStatus.RejectedId;
    public const int StatusReturned = MovementStatus.ReturnedId;
    public const int StatusRedirected = MovementStatus.RedirectedId;
    public const int StatusClosed = MovementStatus.ClosedId;
    public const int StatusCanceled = MovementStatus.CanceledId;

    private readonly List<DocumentMovementStep> _steps = new();
    private readonly List<DocumentMovementEvent> _events = new();

    public Guid MovementId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid DocumentId { get; private set; }
    // FK → MovementType.Id (InstitutionalDiwan.Domain.Entities.Movements.MovementType)
    public int MovementTypeId { get; private set; } = MovementType.ReferralId;
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid SourceDepartmentId { get; private set; }
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    public Guid DestinationDepartmentId { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid? AssignedToUserId { get; private set; }
    // FK → MovementStatus.Id (InstitutionalDiwan.Domain.Entities.Movements.MovementStatus)
    public int StatusId { get; private set; } = StatusDraft;
    public int CurrentStepNo { get; private set; }
    public string? Instructions { get; private set; }
    public DateTime? DueDate { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
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
        int movementTypeId = MovementType.ReferralId,
        Guid? assignedToUserId = null,
        string? instructions = null,
        DateTime? dueDate = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        if (sourceDepartmentId == destinationDepartmentId && !assignedToUserId.HasValue)
            return Result.Failure<DocumentMovement>(DomainErrors.Movements.SameSourceAndDestination);

        var movement = new DocumentMovement
        {
            MovementId = Guid.NewGuid(),
            DocumentId = documentId,
            MovementTypeId = movementTypeId > 0 ? movementTypeId : MovementType.ReferralId,
            SourceDepartmentId = sourceDepartmentId,
            DestinationDepartmentId = destinationDepartmentId,
            AssignedToUserId = assignedToUserId,
            StatusId = StatusDraft,
            CurrentStepNo = 1,
            Instructions = instructions,
            DueDate = dueDate,
            CreatedByUserId = createdByUserId,
            CreatedAt = now
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
        movement.AppendEvent(1, MovementEventType.CreatedId, createdByUserId, instructions, now);
        movement.RaiseDomainEvent(new MovementCreatedEvent(movement.MovementId, movement.DocumentId, sourceDepartmentId, destinationDepartmentId));

        return Result.Success(movement);
    }

    public Result Send(Guid actorUserId, string? notes = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (StatusId is not (StatusDraft or StatusRedirected))
            return Result.Failure(DomainErrors.Movements.InvalidStatusTransition);

        var step = GetCurrentStep();
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        step.MarkSent(now);
        StatusId = StatusSent;
        AppendEvent(CurrentStepNo, MovementEventType.SentId, actorUserId, notes, now);
        RaiseDomainEvent(new MovementSentEvent(MovementId, DocumentId, CurrentStepNo, actorUserId));
        return Result.Success();
    }

    /// <summary>
    /// Receives the current step. A receipt is only valid together with the department register
    /// line the destination department wrote for this document, so a department can never take
    /// custody without numbering it in its own register.
    /// </summary>
    public Result ReceiveStep(Guid actorUserId, Guid departmentRegistryNumberId, string? notes = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (StatusId != StatusSent)
            return Result.Failure(DomainErrors.Movements.InvalidStatusTransition);

        if (departmentRegistryNumberId == Guid.Empty)
            return Result.Failure(DomainErrors.Numbering.DepartmentRegistrationRequired);

        var step = GetCurrentStep();
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        step.MarkReceived(actorUserId, departmentRegistryNumberId, now);
        StatusId = StatusReceived;
        AppendEvent(CurrentStepNo, MovementEventType.ReceivedId, actorUserId, notes, now);
        RaiseDomainEvent(new MovementReceivedEvent(MovementId, DocumentId, CurrentStepNo, actorUserId));
        return Result.Success();
    }

    public Result CompleteStep(Guid actorUserId, string? notes = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (StatusId is not (StatusReceived or StatusInProgress))
            return Result.Failure(DomainErrors.Movements.InvalidStatusTransition);

        var step = GetCurrentStep();
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        step.MarkCompleted(notes, now);
        StatusId = StatusCompleted;
        AppendEvent(CurrentStepNo, MovementEventType.CompletedId, actorUserId, notes, now);
        RaiseDomainEvent(new MovementCompletedEvent(MovementId, DocumentId, CurrentStepNo, actorUserId));
        return Result.Success();
    }

    public Result RejectStep(Guid actorUserId, string? reason, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (StatusId is not (StatusSent or StatusReceived or StatusInProgress))
            return Result.Failure(DomainErrors.Movements.InvalidStatusTransition);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Movements.ReasonRequired);

        var step = GetCurrentStep();
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        step.MarkRejected(reason.Trim(), now);
        StatusId = StatusRejected;
        AppendEvent(CurrentStepNo, MovementEventType.RejectedId, actorUserId, reason.Trim(), now);
        RaiseDomainEvent(new MovementRejectedEvent(MovementId, DocumentId, CurrentStepNo, actorUserId, reason.Trim()));
        return Result.Success();
    }

    public Result ReturnToSender(Guid actorUserId, string? reason, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (StatusId is not (StatusReceived or StatusInProgress))
            return Result.Failure(DomainErrors.Movements.InvalidStatusTransition);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Movements.ReasonRequired);

        var step = GetCurrentStep();
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        step.MarkReturned(reason.Trim(), now);
        StatusId = StatusReturned;
        AppendEvent(CurrentStepNo, MovementEventType.ReturnedId, actorUserId, reason.Trim(), now);
        RaiseDomainEvent(new MovementReturnedEvent(MovementId, DocumentId, CurrentStepNo, actorUserId, reason.Trim()));
        return Result.Success();
    }

    public Result RedirectTo(Guid newDestinationDepartmentId, Guid actorUserId, Guid? assignedToUserId = null, string? instructions = null, DateTime? dueDate = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (StatusId is StatusClosed or StatusCanceled)
            return Result.Failure(DomainErrors.Movements.AlreadyTerminal);

        var step = GetCurrentStep();
        if (step is null)
            return Result.Failure(DomainErrors.Movements.NoActiveStep);

        step.MarkRedirected(instructions, now);

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
        StatusId = StatusSent;
        nextStep.MarkSent(now);

        AppendEvent(nextStepNo, MovementEventType.RedirectedId, actorUserId, instructions, now);
        RaiseDomainEvent(new MovementRedirectedEvent(MovementId, DocumentId, nextStepNo, newDestinationDepartmentId, actorUserId));
        return Result.Success();
    }

    public Result Close(Guid actorUserId, string? notes = null, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (StatusId is StatusClosed or StatusCanceled)
            return Result.Failure(DomainErrors.Movements.AlreadyTerminal);

        StatusId = StatusClosed;
        ClosedAt = now;
        AppendEvent(CurrentStepNo, MovementEventType.ClosedId, actorUserId, notes, now);
        RaiseDomainEvent(new MovementClosedEvent(MovementId, DocumentId, actorUserId));
        return Result.Success();
    }

    public Result Cancel(Guid actorUserId, string? reason, DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (StatusId is StatusClosed or StatusCanceled)
            return Result.Failure(DomainErrors.Movements.AlreadyTerminal);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(DomainErrors.Movements.ReasonRequired);

        StatusId = StatusCanceled;
        ClosedAt = now;
        AppendEvent(CurrentStepNo, MovementEventType.CanceledId, actorUserId, reason.Trim(), now);
        RaiseDomainEvent(new MovementCancelledEvent(MovementId, DocumentId, actorUserId, reason.Trim()));
        return Result.Success();
    }

    private DocumentMovementStep? GetCurrentStep() =>
        _steps.FirstOrDefault(s => s.StepNo == CurrentStepNo);

    private void AppendEvent(int stepNo, int eventTypeId, Guid actorUserId, string? notes, DateTime utcNow)
    {
        _events.Add(DocumentMovementEvent.Create(MovementId, stepNo, eventTypeId, actorUserId, notes, utcNow));
    }
}
