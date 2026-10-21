namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>Payload submitted to <c>POST /api/movements/{movementId}/cancel</c>.</summary>
public sealed record CancelMovementRequest(string Reason);
