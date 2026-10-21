using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Closes the movement: the document is no longer in circulation. The document leaves the
/// IN_MOVEMENT status and returns to REGISTERED.
/// </summary>
public sealed record CloseMovementCommand(
    Guid MovementId,
    Guid UserId,
    string? Notes = null) : ICommand<MovementResponse>;
