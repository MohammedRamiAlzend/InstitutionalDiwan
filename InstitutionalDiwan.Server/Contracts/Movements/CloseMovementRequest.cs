namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>Payload submitted to <c>POST /api/movements/{movementId}/close</c>.</summary>
public sealed record CloseMovementRequest(string? Notes = null);
