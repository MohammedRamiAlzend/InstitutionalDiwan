using InstitutionalDiwan.Domain.Entities.Organization;

namespace InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;

/// <summary>Read model returned by the institution settings endpoints (a single row).</summary>
public sealed record InstitutionSettingResponse(
    Guid SettingId,
    string InstitutionNameAr,
    string? InstitutionNameEn,
    string? LogoPath,
    int NumberingModeId,
    int NumberingResetPolicyId,
    string DefaultTimezone,
    Guid? CentralDiwanDepartmentId,
    Guid? ResponsibleEmployeeId,
    DateTime UpdatedAt)
{
    public static InstitutionSettingResponse From(InstitutionSetting setting) => new(
        setting.SettingId,
        setting.InstitutionNameAr,
        setting.InstitutionNameEn,
        setting.LogoPath,
        setting.NumberingModeId,
        setting.NumberingResetPolicyId,
        setting.DefaultTimezone,
        setting.CentralDiwanDepartmentId,
        setting.ResponsibleEmployeeId,
        setting.UpdatedAt);
}
