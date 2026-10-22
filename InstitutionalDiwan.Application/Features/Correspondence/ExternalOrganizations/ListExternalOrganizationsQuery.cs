using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

/// <summary>
/// A page of external organizations ordered by code, optionally filtered by a code or name search
/// and, unless <paramref name="IncludeInactive"/> is true, restricted to active organizations.
/// </summary>
public sealed record ListExternalOrganizationsQuery(
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    bool IncludeInactive = false) : IQuery<PagedResult<ExternalOrganizationResponse>>;
