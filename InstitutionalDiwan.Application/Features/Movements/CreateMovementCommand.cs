using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Entities.Movements;

namespace InstitutionalDiwan.Application.Features.Movements;

/// <summary>
/// Routes a document to another department by opening a movement. The source department is always
/// the caller's primary department — the caller routes the document out of their own section — and
/// the destination is the department the document is handed to.
/// <para>
/// A document can have one active movement at a time, and only a REGISTERED document can be routed.
/// </para>
/// </summary>
public sealed record CreateMovementCommand(
    Guid DocumentId,
    Guid UserId,
    Guid DestinationDepartmentId,
    int MovementTypeId = MovementType.ReferralId,
    Guid? AssignedToUserId = null,
    string? Instructions = null,
    DateTime? DueDate = null) : ICommand<MovementResponse>;
