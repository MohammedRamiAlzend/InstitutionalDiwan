using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Attachments;

/// <summary>Lists a document's files, oldest first, each with every version it has held.</summary>
public sealed record ListDocumentFilesQuery(Guid DocumentId, Guid UserId)
    : IQuery<IReadOnlyCollection<DocumentFileResponse>>;
