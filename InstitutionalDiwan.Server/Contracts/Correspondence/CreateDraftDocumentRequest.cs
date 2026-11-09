namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>
/// Payload submitted to <c>POST /api/documents/drafts</c>. The document is created in DRAFT status
/// and receives no official number until it is registered.
/// </summary>
public sealed record CreateDraftDocumentRequest(
    string? Title,
    int DocumentTypeId,
    string? SubjectSummary = null,
    int? DirectionId = null,
    int? ConfidentialityLevelId = null,
    int? PriorityLevelId = null,
    DateTime? DocumentDate = null,
    string? ExternalReferenceNumber = null,
    DateTime? ExternalDocumentDate = null,
    IReadOnlyList<DocumentFieldValueRequest>? FieldValues = null,
    IReadOnlyList<InstitutionalDiwan.Server.Contracts.Persons.DocumentPersonRequest>? Persons = null);
