using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Redirects the document to another department without closing the movement: the current step is
/// marked as redirected and a new, already-sent step is appended for the new destination.
/// <paramref name="MovementTypeId"/> restates what the document travels for from this hop on (for
/// example "to archive"); null keeps the movement's current type.
/// </summary>
public sealed record RedirectMovementCommand(
    Guid MovementId,
    Guid UserId,
    Guid NewDestinationDepartmentId,
    Guid? AssignedToUserId = null,
    string? Instructions = null,
    DateTime? DueDate = null,
    int? MovementTypeId = null) : ICommand<MovementResponse>;
