namespace InstitutionalDiwan.Server.Contracts.Movements;

/// <summary>
/// Payload submitted to <c>POST /api/movements/{movementId}/receive</c>. When the department already
/// has a number for the document in the year of <c>RegistryDate</c> that number is reused; otherwise
/// <c>RegistryNumberText</c> is required.
/// </summary>
public sealed record ReceiveDocumentRequest(
    string? Notes = null,
    DateTime? RegistryDate = null,
    string? RegistryNumberText = null,
    Guid? RegistryId = null);
