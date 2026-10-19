using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

/// <summary>
/// Updates an external organization's descriptive fields. The code cannot change because it is the
/// stable business key referenced by correspondence.
/// </summary>
public sealed record UpdateExternalOrganizationCommand(
    Guid ExternalOrganizationId,
    string NameAr,
    int OrganizationTypeId,
    string? NameEn = null,
    string? ContactInfo = null) : ICommand<ExternalOrganizationResponse>;
