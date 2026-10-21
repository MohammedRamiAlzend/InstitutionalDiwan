namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>Payload submitted to <c>POST /api/movements/{movementId}/complete</c>.</summary>
public sealed record CompleteMovementStepRequest(string? Notes = null);
