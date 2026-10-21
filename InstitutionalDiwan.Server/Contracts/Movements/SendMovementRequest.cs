namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>Payload submitted to <c>POST /api/movements/{movementId}/send</c>.</summary>
public sealed record SendMovementRequest(string? Notes = null);
