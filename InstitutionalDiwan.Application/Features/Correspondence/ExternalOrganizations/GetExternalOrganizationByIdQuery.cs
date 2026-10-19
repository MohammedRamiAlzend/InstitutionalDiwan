using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

/// <summary>Fetches a single external organization by id.</summary>
public sealed record GetExternalOrganizationByIdQuery(Guid ExternalOrganizationId)
    : IQuery<ExternalOrganizationResponse>;
