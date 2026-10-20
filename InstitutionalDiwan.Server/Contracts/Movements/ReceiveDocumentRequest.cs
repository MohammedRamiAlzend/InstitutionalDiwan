namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>
/// Payload submitted to <c>POST /api/movements/{movementId}/receive</c>. The department register
/// entry must already exist, otherwise the receipt is rejected.
/// </summary>
public sealed record ReceiveDocumentRequest(string? Notes = null);
