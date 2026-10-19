namespace InstitutionalDiwan.Server.Contracts.Numbering;

/// <summary>Payload submitted to <c>POST /api/documents/{documentId}/numbers/{numberId}/replace</c>.</summary>
public sealed record ReplaceOfficialNumberRequest(
    string Reason,
    int? NumberingYear = null);
