namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>
/// Payload submitted to <c>POST /api/documents</c>. The type/direction/confidentiality/priority
/// fields are ids into the corresponding seeded lookup tables.
/// </summary>
public sealed record CreateDocumentRequest(
    string Title,
    int DocumentTypeId,
    int DirectionId,
    string OfficialNumberText,
    int NumberingYear,
    string? SubjectSummary = null,
    int? ConfidentialityLevelId = null,
    int? PriorityLevelId = null);
