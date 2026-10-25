using InstitutionalDiwan.Domain.Entities.Attachments;

namespace InstitutionalDiwan.Application.Features.Attachments;

/// <summary>One immutable numbered version of a document file, with its checksum and storage path.</summary>
public sealed record DocumentFileVersionResponse(
    Guid VersionId,
    int VersionNumber,
    int StorageProviderId,
    string RelativePath,
    string OriginalFileName,
    string MimeType,
    long FileSizeBytes,
    string ChecksumSha256,
    bool IsCurrent,
    Guid UploadedByUserId,
    string? ChangeNotes,
    DateTime UploadedAt)
{
    public static DocumentFileVersionResponse From(DocumentFileVersion version) => new(
        version.VersionId,
        version.VersionNumber,
        version.StorageProviderId,
        version.RelativePath,
        version.OriginalFileName,
        version.MimeType,
        version.FileSizeBytes,
        version.ChecksumSha256,
        version.IsCurrent,
        version.UploadedByUserId,
        version.ChangeNotes,
        version.UploadedAt);
}

/// <summary>A document file with the full history of its versions.</summary>
public sealed record DocumentFileResponse(
    Guid FileId,
    Guid DocumentId,
    int FileTypeId,
    string Title,
    bool IsPrimaryScan,
    int CurrentVersionNumber,
    bool IsArchived,
    Guid CreatedByUserId,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyCollection<DocumentFileVersionResponse> Versions)
{
    public static DocumentFileResponse From(DocumentFile file) => new(
        file.FileId,
        file.DocumentId,
        file.FileTypeId,
        file.Title,
        file.IsPrimaryScan,
        file.CurrentVersionNumber,
        file.IsArchived,
        file.CreatedByUserId,
        file.CreatedAt,
        file.UpdatedAt,
        file.Versions
            .OrderByDescending(version => version.VersionNumber)
            .Select(DocumentFileVersionResponse.From)
            .ToList());
}
