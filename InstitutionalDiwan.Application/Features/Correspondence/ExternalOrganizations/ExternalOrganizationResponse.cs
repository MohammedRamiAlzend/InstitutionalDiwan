using InstitutionalDiwan.Domain.Entities.Correspondence;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

/// <summary>Read model returned by the external organization endpoints.</summary>
public sealed record ExternalOrganizationResponse(
    Guid ExternalOrganizationId,
    string Code,
    string NameAr,
    string? NameEn,
    int OrganizationTypeId,
    string? ContactInfo,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static ExternalOrganizationResponse From(ExternalOrganization organization) => new(
        organization.ExternalOrganizationId,
        organization.Code,
        organization.NameAr,
        organization.NameEn,
        organization.OrganizationTypeId,
        organization.ContactInfo,
        organization.IsActive,
        organization.CreatedAt,
        organization.UpdatedAt);
}
