namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>Payload submitted to <c>POST /api/documents/{documentId}/cancel</c>; the reason is required.</summary>
public sealed record CancelDocumentRequest(string Reason);
