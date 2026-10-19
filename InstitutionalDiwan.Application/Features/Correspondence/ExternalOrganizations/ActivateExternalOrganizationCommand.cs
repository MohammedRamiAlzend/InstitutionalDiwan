using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

/// <summary>Reactivates a deactivated external organization so it can be selected again.</summary>
public sealed record ActivateExternalOrganizationCommand(Guid ExternalOrganizationId)
    : ICommand<ExternalOrganizationResponse>;
