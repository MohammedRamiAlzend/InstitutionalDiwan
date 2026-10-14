using System;
using InstitutionalDiwan.Domain.Common;
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
    public string NumberingMode { get; private set; } = null!;
    public string NumberingResetPolicy { get; private set; } = null!;
    public string DefaultTimezone { get; private set; } = "Asia/Damascus";
    public DateTime UpdatedAt { get; private set; }

    private InstitutionSetting()
    {
    }

    public static Result<InstitutionSetting> Create(
        string? institutionNameAr,
        string? numberingMode,
        string? numberingResetPolicy,
        string? institutionNameEn = null,
        string? logoPath = null,
        string defaultTimezone = "Asia/Damascus")
    {
        if (string.IsNullOrWhiteSpace(institutionNameAr))
            return Result.Failure<InstitutionSetting>(DomainErrors.InstitutionSetting.NameEmpty);

        if (string.IsNullOrWhiteSpace(numberingMode))
            return Result.Failure<InstitutionSetting>(DomainErrors.InstitutionSetting.NumberingModeEmpty);

        if (string.IsNullOrWhiteSpace(numberingResetPolicy))
            return Result.Failure<InstitutionSetting>(DomainErrors.InstitutionSetting.ResetPolicyEmpty);

        var setting = new InstitutionSetting
        {
            SettingId = Guid.NewGuid(),
            InstitutionNameAr = institutionNameAr.Trim(),
            InstitutionNameEn = string.IsNullOrWhiteSpace(institutionNameEn) ? null : institutionNameEn.Trim(),
            LogoPath = logoPath,
            NumberingMode = numberingMode.Trim().ToUpperInvariant(),
            NumberingResetPolicy = numberingResetPolicy.Trim().ToUpperInvariant(),
            DefaultTimezone = string.IsNullOrWhiteSpace(defaultTimezone) ? "Asia/Damascus" : defaultTimezone.Trim(),
            UpdatedAt = DateTime.UtcNow
        };

        return Result.Success(setting);
    }

    public Result UpdateNumberingPolicy(string? numberingMode, string? numberingResetPolicy)
    {
        if (string.IsNullOrWhiteSpace(numberingMode))
            return Result.Failure(DomainErrors.InstitutionSetting.NumberingModeEmpty);

        if (string.IsNullOrWhiteSpace(numberingResetPolicy))
            return Result.Failure(DomainErrors.InstitutionSetting.ResetPolicyEmpty);

        NumberingMode = numberingMode.Trim().ToUpperInvariant();
        NumberingResetPolicy = numberingResetPolicy.Trim().ToUpperInvariant();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
