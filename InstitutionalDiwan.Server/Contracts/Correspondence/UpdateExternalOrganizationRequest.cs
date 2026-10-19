namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>
/// Payload submitted to <c>PUT /api/external-organizations/{externalOrganizationId}</c>. The code is
/// immutable, so it is not part of the payload.
/// </summary>
public sealed record UpdateExternalOrganizationRequest(
    string NameAr,
    int OrganizationTypeId,
    string? NameEn = null,
    string? ContactInfo = null);
