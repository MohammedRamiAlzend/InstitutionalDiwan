namespace InstitutionalDiwan.Server.Contracts.Correspondence;

/// <summary>
/// Payload submitted to <c>POST /api/external-organizations</c>. <c>ContactInfo</c> must be an
/// e-mail address or a phone number when supplied.
/// </summary>
public sealed record CreateExternalOrganizationRequest(
    string Code,
    string NameAr,
    int OrganizationTypeId,
    string? NameEn = null,
    string? ContactInfo = null);
