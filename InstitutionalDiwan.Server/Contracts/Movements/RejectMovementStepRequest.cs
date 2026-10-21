namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>Payload submitted to <c>POST /api/movements/{movementId}/reject</c>.</summary>
public sealed record RejectMovementStepRequest(string Reason);
