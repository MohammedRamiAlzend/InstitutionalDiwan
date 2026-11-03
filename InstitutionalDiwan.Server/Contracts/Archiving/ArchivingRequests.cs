namespace InstitutionalDiwan.Server.Contracts.Archiving;

/// <summary>Payload submitted to <c>POST /api/documents/{documentId}/archive</c>; the location is required.</summary>
public sealed record ArchiveDocumentRequest(int ArchiveLocationId, string? Reason = null);

/// <summary>Payload submitted to <c>POST /api/documents/{documentId}/unarchive</c>; the reason is required.</summary>
public sealed record UnarchiveDocumentRequest(string Reason);

/// <summary>Payload submitted to create or replace a saved archive search.</summary>
public sealed record SaveSearchRequest(string Name, string CriteriaJson, bool IsDefault = false);
