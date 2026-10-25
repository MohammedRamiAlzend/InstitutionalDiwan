using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Attachments;

/// <summary>
/// Creates a document file and stores its first version in one use case. This is the atomic
/// initial upload; later versions are appended with <see cref="UploadDocumentFileVersionCommand"/>.
/// </summary>
public sealed record CreateDocumentFileWithVersionCommand(
    Guid DocumentId,
    int FileTypeId,
    string? Title,
    Guid UserId,
    Stream Content,
    string OriginalFileName,
    string? MimeType,
    long FileSizeBytes,
    bool IsPrimaryScan = false,
    string? ChangeNotes = null) : ICommand<DocumentFileResponse>;
