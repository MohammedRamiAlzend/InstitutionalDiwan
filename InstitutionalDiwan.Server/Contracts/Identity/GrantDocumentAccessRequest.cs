namespace InstitutionalDiwan.Server.Contracts.Identity;

/// <summary>Payload submitted to <c>POST /api/documents/{documentId}/access-grants</c>.</summary>
public sealed record GrantDocumentAccessRequest(
    Guid UserId,
    int AccessLevelId,
    string? Reason = null,
    DateTime? ExpiresAt = null);
