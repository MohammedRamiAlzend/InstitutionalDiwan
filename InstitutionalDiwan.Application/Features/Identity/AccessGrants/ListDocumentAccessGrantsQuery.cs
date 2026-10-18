using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Identity.AccessGrants;

/// <summary>Lists every access grant recorded against a document, newest first.</summary>
public sealed record ListDocumentAccessGrantsQuery(Guid DocumentId)
    : IQuery<IReadOnlyCollection<DocumentAccessGrantResponse>>;
