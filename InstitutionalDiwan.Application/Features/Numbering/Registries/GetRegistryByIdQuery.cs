using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>Fetches a single numbering registry by its id.</summary>
public sealed record GetRegistryByIdQuery(Guid RegistryId) : IQuery<NumberingRegistryResponse>;
