using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Rejects the movement's current step. A reason is mandatory and is kept on the step and in the
/// movement's audit trail.
/// </summary>
public sealed record RejectMovementStepCommand(
    Guid MovementId,
    Guid UserId,
    string Reason) : ICommand<MovementResponse>;
