using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>
/// Deactivates a registry so it stops being selected for new documents. Numbers already issued from
/// it remain untouched.
/// </summary>
public sealed record DeactivateNumberingRegistryCommand(Guid RegistryId)
    : ICommand<NumberingRegistryResponse>;
