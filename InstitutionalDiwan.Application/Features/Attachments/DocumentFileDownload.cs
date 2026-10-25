namespace InstitutionalDiwan.Application.Features.Attachments;

/// <summary>
/// The bytes of one file version plus the metadata needed to set the HTTP response headers
/// (<c>Content-Type</c> and <c>Content-Disposition</c>).
/// </summary>
public sealed record DocumentFileDownload(
    Stream Content,
    string OriginalFileName,
    string MimeType,
    long FileSizeBytes);
