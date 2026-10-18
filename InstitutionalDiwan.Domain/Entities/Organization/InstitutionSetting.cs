using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Organization;

public sealed class InstitutionSetting : AggregateRoot<Guid>
{
    public Guid SettingId
    {
        get => Id;
        private set => Id = value;
    }

    public string InstitutionNameAr { get; private set; } = null!;
    public string? InstitutionNameEn { get; private set; }
    public string? LogoPath { get; private set; }
    // FK → NumberingMode.Id (InstitutionalDiwan.Domain.Entities.Organization.NumberingMode)
    public int NumberingModeId { get; private set; }
    // FK → NumberingResetPolicy.Id (InstitutionalDiwan.Domain.Entities.Numbering.NumberingResetPolicy)
    public int NumberingResetPolicyId { get; private set; }
    public string DefaultTimezone { get; private set; } = "Asia/Damascus";
    public DateTime UpdatedAt { get; private set; }

    private InstitutionSetting()
    {
    }

    public static Result<InstitutionSetting> Create(
        string? institutionNameAr,
        int numberingModeId,
        int numberingResetPolicyId,
        string? institutionNameEn = null,
        string? logoPath = null,
        string defaultTimezone = "Asia/Damascus")
    {
        if (string.IsNullOrWhiteSpace(institutionNameAr))
            return Result.Failure<InstitutionSetting>(DomainErrors.InstitutionSetting.NameEmpty);

        if (numberingModeId <= 0)
            return Result.Failure<InstitutionSetting>(DomainErrors.InstitutionSetting.NumberingModeEmpty);

        if (numberingResetPolicyId <= 0)
            return Result.Failure<InstitutionSetting>(DomainErrors.InstitutionSetting.ResetPolicyEmpty);

        var setting = new InstitutionSetting
        {
            SettingId = Guid.NewGuid(),
            InstitutionNameAr = institutionNameAr.Trim(),
            InstitutionNameEn = string.IsNullOrWhiteSpace(institutionNameEn) ? null : institutionNameEn.Trim(),
            LogoPath = logoPath,
            NumberingModeId = numberingModeId,
            NumberingResetPolicyId = numberingResetPolicyId,
            DefaultTimezone = string.IsNullOrWhiteSpace(defaultTimezone) ? "Asia/Damascus" : defaultTimezone.Trim(),
            UpdatedAt = DateTime.UtcNow
        };

        return Result.Success(setting);
    }

    public Result UpdateNumberingPolicy(int numberingModeId, int numberingResetPolicyId)
    {
        if (numberingModeId <= 0)
            return Result.Failure(DomainErrors.InstitutionSetting.NumberingModeEmpty);

        if (numberingResetPolicyId <= 0)
            return Result.Failure(DomainErrors.InstitutionSetting.ResetPolicyEmpty);

        NumberingModeId = numberingModeId;
        NumberingResetPolicyId = numberingResetPolicyId;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
