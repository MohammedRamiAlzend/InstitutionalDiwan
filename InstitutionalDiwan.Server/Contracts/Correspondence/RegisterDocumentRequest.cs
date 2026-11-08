namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>
/// Payload submitted to <c>POST /api/documents/{documentId}/register</c>. Omit
/// <c>NumberingYear</c> to use the document's year (or the current year); the system number is a GUID issued by the server. Supply <c>DepartmentRegistryNumberText</c> and <c>DepartmentRegistryDate</c> to write the caller's department register line, and optionally <c>Movement</c> to send the document on.
/// </summary>
public sealed record RegisterDocumentRequest(
    int? NumberingYear = null,
    string? DepartmentRegistryNumberText = null,
    DateTime? DepartmentRegistryDate = null,
    CreateDocumentMovementRequest? Movement = null,
    Guid? DepartmentRegistryId = null);
