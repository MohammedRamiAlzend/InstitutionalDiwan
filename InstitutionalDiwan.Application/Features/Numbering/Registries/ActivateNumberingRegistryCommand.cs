using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>Reactivates a registry so it can be selected for new documents again.</summary>
public sealed record ActivateNumberingRegistryCommand(Guid RegistryId)
    : ICommand<NumberingRegistryResponse>;
