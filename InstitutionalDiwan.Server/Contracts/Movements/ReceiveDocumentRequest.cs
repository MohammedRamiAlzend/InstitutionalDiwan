using InstitutionalDiwan.Server.Contracts.Correspondence;

namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>
/// Payload submitted to <c>POST /api/movements/{movementId}/receive</c>. When the department already
/// has a number for the document in the year of <c>RegistryDate</c> that number is reused; otherwise
/// <c>RegistryNumberText</c> is required. <c>FieldValues</c> fills the department's record form for
/// the document type when one is published; its required fields must be filled. Set
/// <c>IssueNewNumber</c> to write the document under a new <c>RegistryNumberText</c> although the
/// department already has a number for it that year; the earlier number becomes <c>REPLACED</c> and
/// stays linked to the new one.
/// </summary>
public sealed record ReceiveDocumentRequest(
    string? Notes = null,
    DateTime? RegistryDate = null,
    string? RegistryNumberText = null,
    Guid? RegistryId = null,
    IReadOnlyList<DocumentFieldValueRequest>? FieldValues = null,
    bool IssueNewNumber = false);
