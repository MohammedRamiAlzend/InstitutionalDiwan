using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Sends the movement's current step to the destination department. Only a movement in DRAFT (or
/// REDIRECTED) status can be sent.
/// </summary>
public sealed record SendMovementCommand(
    Guid MovementId,
    Guid UserId,
    string? Notes = null) : ICommand<MovementResponse>;
