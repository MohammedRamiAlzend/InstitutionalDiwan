using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;

namespace InstitutionalDiwan.Application.Features.Relations;

/// <summary>
/// A page of a document's relation history, newest first. It includes links that were later removed,
/// which no longer appear among the document's relations.
/// </summary>
public sealed record ListDocumentRelationEventsQuery(
    Guid DocumentId,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<DocumentRelationEventResponse>>;
