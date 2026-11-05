namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>
/// Payload submitted to <c>POST /api/documents</c>. The type is an id into the seeded document types;
/// direction, confidentiality and priority are suspended and optional (null by default).
/// The central system number (a GUID) and its year are issued by the server. The department register
/// number and date are written into the caller's own department register; an optional movement sends
/// the document on immediately.
/// </summary>
public sealed record CreateDocumentRequest(
    string Title,
    int DocumentTypeId,
    string DepartmentRegistryNumberText,
    DateTime DepartmentRegistryDate,
    string? SubjectSummary = null,
    int? DirectionId = null,
    int? ConfidentialityLevelId = null,
    int? PriorityLevelId = null,
    IReadOnlyList<DocumentFieldValueRequest>? FieldValues = null,
    CreateDocumentMovementRequest? Movement = null);

/// <summary>Destination of the movement opened and sent together with the new document.</summary>
public sealed record CreateDocumentMovementRequest(
    Guid DestinationDepartmentId,
    int MovementTypeId = 1,
    Guid? AssignedToUserId = null,
    string? Instructions = null,
    DateTime? DueDate = null);
