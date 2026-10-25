using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Attachments;

/// <summary>
/// Registers metadata for a new document file (a scan, attachment, signature, or other file type).
/// The binary content is added afterwards with <see cref="UploadDocumentFileVersionCommand"/>; this
/// command only opens the version chain.
/// </summary>
public sealed record CreateDocumentFileCommand(
    Guid DocumentId,
    int FileTypeId,
    string? Title,
    Guid UserId,
    bool IsPrimaryScan = false) : ICommand<DocumentFileResponse>;
