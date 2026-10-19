namespace InstitutionalDiwan.Server.Contracts.Numbering;

/// <summary>
/// Payload submitted to <c>POST /api/numbering/registries</c>. Omit <c>FormatPattern</c> to use the
/// default <c>{YEAR}/{SEQ:D6}</c> template.
/// </summary>
public sealed record CreateNumberingRegistryRequest(
    string Code,
    string NameAr,
    string? FormatPattern = null,
    int ResetPolicyId = 0,
    Guid? DepartmentId = null,
    int? DocumentTypeId = null,
    int? DirectionId = null);
