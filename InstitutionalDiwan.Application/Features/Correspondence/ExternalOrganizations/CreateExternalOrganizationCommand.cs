using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

/// <summary>
/// Registers an external organization (ministry, company, citizen, …) so it can participate in
/// external correspondence. The code is normalized to upper case and must be unique.
/// </summary>
public sealed record CreateExternalOrganizationCommand(
    string Code,
    string NameAr,
    int OrganizationTypeId,
    string? NameEn = null,
    string? ContactInfo = null) : ICommand<ExternalOrganizationResponse>;
