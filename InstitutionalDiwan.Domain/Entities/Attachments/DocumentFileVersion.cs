namespace InstitutionalDiwan.Domain.Entities.Attachments;

public sealed class DocumentFileVersion : Entity<Guid>
{
    public Guid VersionId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DocumentFile.FileId (InstitutionalDiwan.Domain.Entities.Attachments.DocumentFile)
    public Guid FileId { get; private set; }
    public int VersionNumber { get; private set; }
    // FK → FileStorageProvider.Id (InstitutionalDiwan.Domain.Entities.Attachments.FileStorageProvider)
    public int StorageProviderId { get; private set; } = FileStorageProvider.LocalDefaultId;
    public string RelativePath { get; private set; } = null!;
    public string OriginalFileName { get; private set; } = null!;
    public string MimeType { get; private set; } = null!;
    public long FileSizeBytes { get; private set; }
    public string ChecksumSha256 { get; private set; } = null!;
    public bool IsCurrent { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid UploadedByUserId { get; private set; }
    public string? ChangeNotes { get; private set; }
    public DateTime UploadedAt { get; private set; }

    private DocumentFileVersion()
    {
    }

    internal static Result<DocumentFileVersion> Create(
        Guid fileId,
        int versionNumber,
        int storageProviderId,
        string? relativePath,
        string? originalFileName,
        string? mimeType,
        long fileSizeBytes,
        string? checksumSha256,
        Guid uploadedByUserId,
        string? changeNotes = null)
    {
        if (versionNumber <= 0)
            return Result.Failure<DocumentFileVersion>(DomainErrors.Attachments.InvalidVersionNumber);

        if (string.IsNullOrWhiteSpace(relativePath))
            return Result.Failure<DocumentFileVersion>(DomainErrors.Attachments.StoragePathEmpty);

        if (fileSizeBytes <= 0)
            return Result.Failure<DocumentFileVersion>(DomainErrors.Attachments.FileSizeInvalid);

        if (string.IsNullOrWhiteSpace(checksumSha256) || checksumSha256.Trim().Length != 64)
            return Result.Failure<DocumentFileVersion>(DomainErrors.Attachments.ChecksumInvalid);

        var version = new DocumentFileVersion
        {
            VersionId = Guid.NewGuid(),
            FileId = fileId,
            VersionNumber = versionNumber,
            StorageProviderId = storageProviderId > 0 ? storageProviderId : FileStorageProvider.LocalDefaultId,
            RelativePath = relativePath.Trim(),
            OriginalFileName = string.IsNullOrWhiteSpace(originalFileName) ? "document.pdf" : originalFileName.Trim(),
            MimeType = string.IsNullOrWhiteSpace(mimeType) ? "application/pdf" : mimeType.Trim(),
            FileSizeBytes = fileSizeBytes,
            ChecksumSha256 = checksumSha256.Trim().ToLowerInvariant(),
            IsCurrent = true,
            UploadedByUserId = uploadedByUserId,
            ChangeNotes = changeNotes,
            UploadedAt = DateTime.UtcNow
        };

        return Result.Success(version);
    }

    internal void MarkSuperseded()
    {
        IsCurrent = false;
    }
}
