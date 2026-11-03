using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Relations;

/// <summary>
/// The documents reachable from <paramref name="DocumentId"/> within <paramref name="Depth"/> hops
/// (1 to 3, default 2), through links the caller may see, capped at 100 documents.
/// </summary>
public sealed record GetDocumentRelationsGraphQuery(Guid DocumentId, int Depth = 2)
    : IQuery<RelationGraphResponse>;
