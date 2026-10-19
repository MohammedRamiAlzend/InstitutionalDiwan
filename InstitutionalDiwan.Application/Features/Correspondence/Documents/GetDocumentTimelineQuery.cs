using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Chronological audit trail of a document, oldest first: status transitions, added participants,
/// and official-number issue/cancel/replace events.
/// </summary>
public sealed record GetDocumentTimelineQuery(Guid DocumentId)
    : IQuery<IReadOnlyCollection<DocumentTimelineEntryResponse>>;
