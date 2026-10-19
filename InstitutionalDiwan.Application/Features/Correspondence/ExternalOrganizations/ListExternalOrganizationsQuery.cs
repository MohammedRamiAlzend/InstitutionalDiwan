using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

/// <summary>Lists external organizations by code, optionally including deactivated ones.</summary>
public sealed record ListExternalOrganizationsQuery(bool IncludeInactive = false)
    : IQuery<IReadOnlyCollection<ExternalOrganizationResponse>>;
