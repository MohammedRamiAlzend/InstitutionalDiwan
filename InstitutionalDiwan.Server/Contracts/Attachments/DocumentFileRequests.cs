namespace InstitutionalDiwan.Server.Contracts.Attachments;

/// <summary>
/// Payload submitted to <c>POST /api/document-files</c> to open a new version chain for a document
/// file. The file's bytes are uploaded separately to <c>POST /api/document-files/{fileId}/versions</c>.
/// </summary>
public sealed record CreateDocumentFileRequest(
    Guid DocumentId,
    int FileTypeId,
    string Title,
    bool IsPrimaryScan = false);

/// <summary>
/// Multipart payload submitted to <c>POST /api/document-files/{fileId}/versions</c>. The stream is
/// validated against the file type's allowed extensions, size limit, and magic bytes.
/// </summary>
public sealed record UploadDocumentFileVersionRequest(
    IFormFile File,
    string? ChangeNotes = null);
