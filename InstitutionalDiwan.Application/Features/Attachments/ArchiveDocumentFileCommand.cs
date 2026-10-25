using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Attachments;

/// <summary>
/// Archives a document file so no further versions can be uploaded. The stored versions stay
/// readable; archiving only closes the version chain.
/// </summary>
public sealed record ArchiveDocumentFileCommand(Guid FileId, Guid UserId)
    : ICommand<DocumentFileResponse>;
