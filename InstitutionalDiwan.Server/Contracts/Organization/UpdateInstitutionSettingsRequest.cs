namespace InstitutionalDiwan.Server.Contracts.Organization;

/// <summary>
/// Payload submitted to <c>PUT /api/institution-settings</c>. The Central Diwan must be an active
/// root department of type <c>CENTRAL_DIWAN</c>; changing it requires a transfer reason.
/// </summary>
public sealed record UpdateInstitutionSettingsRequest(
    string InstitutionNameAr,
    int NumberingModeId,
    int NumberingResetPolicyId,
    Guid? CentralDiwanDepartmentId,
    string? InstitutionNameEn = null,
    string? LogoPath = null,
    string? DefaultTimezone = null,
    Guid? ResponsibleEmployeeId = null,
    string? CentralDiwanTransferReason = null);
