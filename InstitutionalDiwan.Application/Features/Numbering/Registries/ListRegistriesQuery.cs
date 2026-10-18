using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>Lists every numbering registry, including its id, scope, and counters.</summary>
public sealed record ListRegistriesQuery : IQuery<IReadOnlyCollection<NumberingRegistryResponse>>;
