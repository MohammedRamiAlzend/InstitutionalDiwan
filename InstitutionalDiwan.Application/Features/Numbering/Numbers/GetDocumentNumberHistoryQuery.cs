using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

/// <summary>
/// Lists every official number ever issued for a document (issued, cancelled, replaced), oldest
/// first, each with its full event history.
/// </summary>
public sealed record GetDocumentNumberHistoryQuery(Guid DocumentId)
    : IQuery<IReadOnlyCollection<DocumentNumberResponse>>;
