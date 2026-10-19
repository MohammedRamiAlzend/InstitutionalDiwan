namespace InstitutionalDiwan.Server.Contracts.Numbering;

/// <summary>
/// Payload submitted to <c>PUT /api/numbering/registries/{registryId}</c>. Changing
/// <c>FormatPattern</c> is rejected once numbers have been issued from the registry.
/// </summary>
public sealed record UpdateNumberingRegistryRequest(
    string NameAr,
    string? FormatPattern = null,
    int ResetPolicyId = 0,
    Guid? DepartmentId = null,
    int? DocumentTypeId = null,
    int? DirectionId = null);
