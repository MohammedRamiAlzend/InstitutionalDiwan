namespace InstitutionalDiwan.Server.Contracts.Numbering;

/// <summary>Payload submitted to <c>POST /api/documents/{documentId}/numbers/{numberId}/cancel</c>.</summary>
public sealed record CancelOfficialNumberRequest(string Reason);
