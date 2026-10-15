namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>Payload submitted to <c>POST /api/documents</c>.</summary>
public sealed record CreateDocumentRequest(
    string Title,
    string DocumentTypeCode,
    string Direction,
    string? SubjectSummary = null,
    string? ConfidentialityLevel = null,
    string? PriorityLevel = null);
