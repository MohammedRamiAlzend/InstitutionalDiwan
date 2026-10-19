namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>
/// Payload submitted to <c>POST /api/documents/{documentId}/register</c>. Omit
/// <c>NumberingYear</c> to allocate in the document's year (or the current year).
/// </summary>
public sealed record RegisterDocumentRequest(int? NumberingYear = null);
