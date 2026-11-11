using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;

/// <summary>
/// Updates the singleton institution settings. The designated Central Diwan must be an active root
/// department of type <c>CENTRAL_DIWAN</c>; moving it away from the current department additionally
/// requires <paramref name="CentralDiwanTransferReason"/>. <paramref name="ArchiveDiwanDepartmentId"/>
/// designates the active department that keeps archived files, or clears it when null.
/// <paramref name="OtherDiwansMayCreateDocuments"/> false reserves document creation to the Central Diwan.
/// </summary>
public sealed record UpdateInstitutionSettingsCommand(
    string InstitutionNameAr,
    int NumberingModeId,
    int NumberingResetPolicyId,
    Guid? CentralDiwanDepartmentId,
    Guid? ResponsibleEmployeeId = null,
    string? InstitutionNameEn = null,
    string? LogoPath = null,
    string? DefaultTimezone = null,
    string? CentralDiwanTransferReason = null,
    Guid? ArchiveDiwanDepartmentId = null,
    bool OtherDiwansMayCreateDocuments = true) : ICommand<InstitutionSettingResponse>;
