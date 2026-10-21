using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Cancels the movement: the routing is called off. A reason is mandatory. The document leaves the
/// IN_MOVEMENT status and returns to REGISTERED.
/// </summary>
public sealed record CancelMovementCommand(
    Guid MovementId,
    Guid UserId,
    string Reason) : ICommand<MovementResponse>;
