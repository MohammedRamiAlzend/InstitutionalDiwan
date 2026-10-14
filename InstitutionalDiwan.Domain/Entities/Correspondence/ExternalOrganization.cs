using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

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
    public string OrganizationType { get; private set; } = "GOVERNMENT";
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
        string organizationType = "GOVERNMENT",
        string? nameEn = null,
        string? contactInfo = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<ExternalOrganization>(DomainErrors.Correspondence.ExternalOrgCodeEmpty);

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<ExternalOrganization>(DomainErrors.Correspondence.ExternalOrgNameEmpty);

        var org = new ExternalOrganization
        {
            ExternalOrganizationId = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            OrganizationType = string.IsNullOrWhiteSpace(organizationType) ? "GOVERNMENT" : organizationType.Trim().ToUpperInvariant(),
            ContactInfo = contactInfo,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(org);
    }

    public Result Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
