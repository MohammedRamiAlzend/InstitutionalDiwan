using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Completes the movement's current step at the destination department. Only a step that was
/// received (or is in progress) can be completed.
/// </summary>
public sealed record CompleteMovementStepCommand(
    Guid MovementId,
    Guid UserId,
    string? Notes = null,
    Guid? StepId = null) : ICommand<MovementResponse>;
