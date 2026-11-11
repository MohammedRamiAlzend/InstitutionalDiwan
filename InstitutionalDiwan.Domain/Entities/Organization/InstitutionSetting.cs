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
    // When the uploaded logo (INSTITUTION_LOGOS) last changed; null while there is none. Doubles as the cache version.
    public DateTime? LogoUpdatedAt { get; private set; }
    // FK → NumberingMode.Id (InstitutionalDiwan.Domain.Entities.Organization.NumberingMode)
    public int NumberingModeId { get; private set; }
    // FK → NumberingResetPolicy.Id (InstitutionalDiwan.Domain.Entities.Numbering.NumberingResetPolicy)
    public int NumberingResetPolicyId { get; private set; }
    public string DefaultTimezone { get; private set; } = "Asia/Damascus";
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    // The root department that acts as the Central Diwan; null until one is designated.
    public Guid? CentralDiwanDepartmentId { get; private set; }
    // FK → Department.DepartmentId (InstitutionalDiwan.Domain.Entities.Organization.Department)
    // The department that keeps archived files; null while archiving stays with the owning department.
    public Guid? ArchiveDiwanDepartmentId { get; private set; }
    // When false, only the Central Diwan creates documents; every other department receives them.
    public bool OtherDiwansMayCreateDocuments { get; private set; } = true;
    // FK → Employee.EmployeeId (InstitutionalDiwan.Domain.Entities.Identity.Employee)
    public Guid? ResponsibleEmployeeId { get; private set; }
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
        string defaultTimezone = "Asia/Damascus",
        Guid? centralDiwanDepartmentId = null,
        Guid? responsibleEmployeeId = null,
        DateTime? utcNow = null)
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
            CentralDiwanDepartmentId = centralDiwanDepartmentId,
            ResponsibleEmployeeId = responsibleEmployeeId,
            UpdatedAt = utcNow ?? DateTime.UtcNow
        };

        return Result.Success(setting);
    }

    /// <summary>
    /// Applies a full settings update. Existence, type, and root checks for the designated Central
    /// Diwan department are performed by the caller (they need repository access); the domain owns
    /// the one rule it can decide alone — moving the Central Diwan away from the current department
    /// requires a documented transfer reason.
    /// </summary>
    public Result Update(
        string? institutionNameAr,
        int numberingModeId,
        int numberingResetPolicyId,
        Guid? centralDiwanDepartmentId,
        Guid? responsibleEmployeeId = null,
        string? institutionNameEn = null,
        string? logoPath = null,
        string? defaultTimezone = null,
        string? centralDiwanTransferReason = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(institutionNameAr))
            return Result.Failure(DomainErrors.InstitutionSetting.NameEmpty);

        if (numberingModeId <= 0)
            return Result.Failure(DomainErrors.InstitutionSetting.NumberingModeEmpty);

        if (numberingResetPolicyId <= 0)
            return Result.Failure(DomainErrors.InstitutionSetting.ResetPolicyEmpty);

        if (!centralDiwanDepartmentId.HasValue)
            return Result.Failure(DomainErrors.InstitutionSetting.CentralDiwanRequired);

        if (CentralDiwanDepartmentId != centralDiwanDepartmentId &&
            string.IsNullOrWhiteSpace(centralDiwanTransferReason))
        {
            return Result.Failure(DomainErrors.InstitutionSetting.CentralDiwanTransferReasonRequired);
        }

        InstitutionNameAr = institutionNameAr.Trim();
        InstitutionNameEn = string.IsNullOrWhiteSpace(institutionNameEn) ? null : institutionNameEn.Trim();
        LogoPath = string.IsNullOrWhiteSpace(logoPath) ? null : logoPath.Trim();
        NumberingModeId = numberingModeId;
        NumberingResetPolicyId = numberingResetPolicyId;
        CentralDiwanDepartmentId = centralDiwanDepartmentId;
        ResponsibleEmployeeId = responsibleEmployeeId;
        DefaultTimezone = string.IsNullOrWhiteSpace(defaultTimezone) ? DefaultTimezone : defaultTimezone.Trim();
        UpdatedAt = now;
        return Result.Success();
    }

    /// <summary>
    /// Designates the archive diwan, or clears it with <c>null</c>. Existence and activity are checked
    /// by the caller; the domain refuses the Central Diwan itself, because the archive route ends at a
    /// department the Central Diwan sends to.
    /// </summary>
    public Result SetArchiveDiwan(Guid? archiveDiwanDepartmentId, DateTime? utcNow = null)
    {
        if (archiveDiwanDepartmentId == Guid.Empty)
            archiveDiwanDepartmentId = null;

        if (archiveDiwanDepartmentId.HasValue && archiveDiwanDepartmentId == CentralDiwanDepartmentId)
            return Result.Failure(DomainErrors.InstitutionSetting.ArchiveDiwanSameAsCentral);

        if (ArchiveDiwanDepartmentId == archiveDiwanDepartmentId)
            return Result.Success();

        ArchiveDiwanDepartmentId = archiveDiwanDepartmentId;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    /// <summary>
    /// Decides whether departments other than the Central Diwan may create documents. When it is off
    /// the Central Diwan alone creates them.
    /// </summary>
    public void SetOtherDiwansMayCreateDocuments(bool allowed, DateTime? utcNow = null)
    {
        if (OtherDiwansMayCreateDocuments == allowed)
            return;

        OtherDiwansMayCreateDocuments = allowed;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }

    /// <summary>True when a user whose department is <paramref name="departmentId"/> may create documents.</summary>
    public bool CanCreateDocuments(Guid departmentId) =>
        OtherDiwansMayCreateDocuments || CentralDiwanDepartmentId == departmentId;

    /// <summary>Records that the uploaded logo was set (a time) or removed (null), so readers can tell it changed.</summary>
    public void MarkLogoChanged(DateTime? logoUpdatedAt, DateTime? utcNow = null)
    {
        LogoUpdatedAt = logoUpdatedAt;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
    }

    public Result UpdateNumberingPolicy(int numberingModeId, int numberingResetPolicyId, DateTime? utcNow = null)
    {
        if (numberingModeId <= 0)
            return Result.Failure(DomainErrors.InstitutionSetting.NumberingModeEmpty);

        if (numberingResetPolicyId <= 0)
            return Result.Failure(DomainErrors.InstitutionSetting.ResetPolicyEmpty);

        NumberingModeId = numberingModeId;
        NumberingResetPolicyId = numberingResetPolicyId;
        UpdatedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }
}
