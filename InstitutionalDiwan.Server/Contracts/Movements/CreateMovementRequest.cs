namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>
/// Payload submitted to <c>POST /api/movements</c>. The source department is the caller's primary
/// department, so only the destination is supplied.
/// </summary>
public sealed record CreateMovementRequest(
    Guid DocumentId,
    Guid DestinationDepartmentId,
    int MovementTypeId = 1,
    Guid? AssignedToUserId = null,
    string? Instructions = null,
    DateTime? DueDate = null);
