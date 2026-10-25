using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Returns the document to the sending department. Only a received (or in-progress) step can be
/// returned, and the reason is mandatory.
/// </summary>
public sealed record ReturnMovementToSenderCommand(
    Guid MovementId,
    Guid UserId,
    string Reason,
    Guid? StepId = null) : ICommand<MovementResponse>;
