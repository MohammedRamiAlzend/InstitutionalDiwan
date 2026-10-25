using InstitutionalDiwan.Application.Abstractions.Messaging;

namespace InstitutionalDiwan.Application.Features.Attachments;

/// <summary>
/// Adds the next immutable version to a document file. The bytes are validated (extension, size,
/// and magic bytes), written to the configured storage provider while their SHA-256 checksum is
/// computed, and then recorded through <c>DocumentFile.AddVersion(...)</c>.
/// </summary>
public sealed record UploadDocumentFileVersionCommand(
    Guid FileId,
    Guid UserId,
    Stream Content,
    string OriginalFileName,
    string? MimeType,
    long FileSizeBytes,
    string? ChangeNotes = null) : ICommand<DocumentFileResponse>;
