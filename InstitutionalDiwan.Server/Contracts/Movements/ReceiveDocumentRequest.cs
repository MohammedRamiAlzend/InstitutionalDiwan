using InstitutionalDiwan.Server.Contracts.Correspondence;

namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>
/// Payload submitted to <c>POST /api/movements/{movementId}/receive</c>. When the department already
/// has a number for the document in the year of <c>RegistryDate</c> that number is reused; otherwise
/// <c>RegistryNumberText</c> is required. <c>FieldValues</c> fills the department's record form for
/// the document type when one is published; its required fields must be filled.
/// </summary>
public sealed record ReceiveDocumentRequest(
    string? Notes = null,
    DateTime? RegistryDate = null,
    string? RegistryNumberText = null,
    Guid? RegistryId = null,
    IReadOnlyList<DocumentFieldValueRequest>? FieldValues = null);
