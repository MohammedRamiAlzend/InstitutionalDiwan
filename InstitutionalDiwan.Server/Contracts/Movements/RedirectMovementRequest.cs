namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>Payload submitted to <c>POST /api/movements/{movementId}/redirect</c>.</summary>
public sealed record RedirectMovementRequest(
    Guid NewDestinationDepartmentId,
    Guid? AssignedToUserId = null,
    string? Instructions = null,
    DateTime? DueDate = null);
