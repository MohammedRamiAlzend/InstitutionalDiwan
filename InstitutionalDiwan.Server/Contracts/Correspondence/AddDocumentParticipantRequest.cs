namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>
/// Payload submitted to <c>POST /api/documents/{documentId}/participants</c>. Supply exactly one of
/// <c>ExternalOrganizationId</c>, <c>InternalDepartmentId</c>, or <c>PersonName</c>.
/// </summary>
public sealed record AddDocumentParticipantRequest(
    int RoleId,
    Guid? ExternalOrganizationId = null,
    Guid? InternalDepartmentId = null,
    string? PersonName = null,
    bool IsPrimary = false,
    string? Notes = null);
