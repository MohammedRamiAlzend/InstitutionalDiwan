namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>Payload submitted to <c>POST /api/movements/{movementId}/return</c>.</summary>
public sealed record ReturnMovementToSenderRequest(string Reason);
