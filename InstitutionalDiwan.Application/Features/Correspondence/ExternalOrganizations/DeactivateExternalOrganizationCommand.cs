using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;

/// <summary>
/// Soft-deletes an external organization. Existing documents keep their participant rows; the
/// organization simply stops being selectable for new correspondence.
/// </summary>
public sealed record DeactivateExternalOrganizationCommand(Guid ExternalOrganizationId)
    : ICommand<ExternalOrganizationResponse>;
