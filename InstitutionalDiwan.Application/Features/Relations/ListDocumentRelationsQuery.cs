using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Relations;

/// <summary>
/// A document's direct links, in both directions. Links to documents the caller cannot view are left
/// out and only counted.
/// </summary>
public sealed record ListDocumentRelationsQuery(Guid DocumentId) : IQuery<DocumentRelationsResponse>;
