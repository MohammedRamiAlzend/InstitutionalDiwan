namespace InstitutionalDiwan.Server.Contracts.Numbering;

/// <summary>
/// Payload submitted to <c>POST /api/documents/{documentId}/numbers/issue</c>. Omit
/// <c>NumberingYear</c> to allocate in the document's year (or the current year).
/// </summary>
public sealed record IssueOfficialNumberRequest(int? NumberingYear = null);
