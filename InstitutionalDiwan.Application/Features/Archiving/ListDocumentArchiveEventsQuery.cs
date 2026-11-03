using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Archiving;

/// <summary>A document's archive history, newest first.</summary>
public sealed record ListDocumentArchiveEventsQuery(Guid DocumentId)
    : IQuery<IReadOnlyCollection<DocumentArchiveEventResponse>>;
