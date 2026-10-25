namespace InstitutionalDiwan.Domain.Entities.Correspondence;

public sealed class ExternalOrganization : AggregateRoot<Guid>
{
    public Guid ExternalOrganizationId
    {
        get => Id;
        private set => Id = value;
    }

    public string Code { get; private set; } = null!;
    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    // FK → OrganizationType.Id (InstitutionalDiwan.Domain.Entities.Correspondence.OrganizationType)
    public int OrganizationTypeId { get; private set; } = OrganizationType.GovernmentId;
    public string? ContactInfo { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    private ExternalOrganization()
    {
    }

    public static Result<ExternalOrganization> Create(
        string? code,
        string? nameAr,
        int organizationTypeId = OrganizationType.GovernmentId,
        string? nameEn = null,
        string? contactInfo = null,
        DateTime? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<ExternalOrganization>(DomainErrors.Correspondence.ExternalOrgCodeEmpty);

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<ExternalOrganization>(DomainErrors.Correspondence.ExternalOrgNameEmpty);

        if (organizationTypeId <= 0)
            return Result.Failure<ExternalOrganization>(DomainErrors.Lookup.InvalidId);

        var org = new ExternalOrganization
        {
            ExternalOrganizationId = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            OrganizationTypeId = organizationTypeId,
            ContactInfo = contactInfo,
            IsActive = true,
            CreatedAt = utcNow ?? DateTime.UtcNow
        };

        return Result.Success(org);
    }

    /// <summary>Updates the descriptive fields; the code is immutable because documents reference it.</summary>
    public Result Update(
        string? nameAr,
        int organizationTypeId,
        string? nameEn = null,
        string? contactInfo = null,
        DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure(DomainErrors.Correspondence.ExternalOrgNameEmpty);

        if (organizationTypeId <= 0)
            return Result.Failure(DomainErrors.Lookup.InvalidId);

        NameAr = nameAr.Trim();
        NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim();
        OrganizationTypeId = organizationTypeId;
        ContactInfo = string.IsNullOrWhiteSpace(contactInfo) ? null : contactInfo.Trim();
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Deactivate(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (!IsActive)
            return Result.Success();

        IsActive = false;
        UpdatedAt = now;
        return Result.Success();
    }

    public Result Activate(DateTime? utcNow = null)
    {
        var now = utcNow ?? DateTime.UtcNow;
        if (IsActive)
            return Result.Success();

        IsActive = true;
        UpdatedAt = now;
        return Result.Success();
    }
}
