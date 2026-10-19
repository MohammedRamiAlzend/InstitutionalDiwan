namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>
/// Payload submitted to <c>PUT /api/documents/{documentId}</c>. Only DRAFT documents accept this
/// update; registered documents reject it with 409.
/// </summary>
public sealed record UpdateDocumentMetadataRequest(
    string Title,
    string? SubjectSummary = null,
    int? ConfidentialityLevelId = null,
    int? PriorityLevelId = null);
