namespace InstitutionalDiwan.Domain.Entities.Drive;

/// <summary>
/// One immutable content version of a Drive file (Module 11). Created Pending for a direct
/// presigned upload, Committed once size and checksum are verified, Abandoned when the upload
/// is cancelled or expires. Committed versions are never modified; restoring an old version
/// creates a new one.
/// </summary>
public sealed class DriveFileVersion : Entity<Guid>
{
    public Guid VersionId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DriveItem.ItemId (a file item)
    public Guid FileItemId { get; private set; }
    public int VersionNumber { get; private set; }
    // FK → FileStorageProvider.Id (InstitutionalDiwan.Domain.Entities.Attachments.FileStorageProvider)
    public int StorageProviderId { get; private set; }
    /// <summary>Object key in the S3-compatible store (SeaweedFS).</summary>
    public string ObjectKey { get; private set; } = null!;
    public long SizeBytes { get; private set; }
    /// <summary>Lower-case 64-char hex SHA-256; null while pending.</summary>
    public string? ChecksumSha256 { get; private set; }
    public string MimeType { get; private set; } = null!;
    public int SourceId { get; private set; }
    public int StatusId { get; private set; }
    // FK → User.UserId
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? CommittedAt { get; private set; }

    public bool IsPending => StatusId == DriveVersionStatus.PendingId;
    public bool IsCommitted => StatusId == DriveVersionStatus.CommittedId;

    private DriveFileVersion()
    {
    }

    public static Result<DriveFileVersion> CreatePending(
        Guid fileItemId,
        int versionNumber,
        int storageProviderId,
        string? objectKey,
        long declaredSizeBytes,
        string? mimeType,
        int sourceId,
        Guid createdByUserId,
        DateTime? utcNow = null)
    {
        if (fileItemId == Guid.Empty)
            return Result.Failure<DriveFileVersion>(DomainErrors.Drive.ItemNotFound);
        if (versionNumber <= 0)
            return Result.Failure<DriveFileVersion>(DomainErrors.Drive.VersionNumberInvalid);
        if (storageProviderId <= 0)
            return Result.Failure<DriveFileVersion>(DomainErrors.Drive.ProviderInvalid);
        if (string.IsNullOrWhiteSpace(objectKey))
            return Result.Failure<DriveFileVersion>(DomainErrors.Drive.ObjectKeyEmpty);
        if (declaredSizeBytes <= 0)
            return Result.Failure<DriveFileVersion>(DomainErrors.Drive.VersionSizeInvalid);
        if (!DriveVersionSource.IsValid(sourceId))
            return Result.Failure<DriveFileVersion>(DomainErrors.Drive.VersionNumberInvalid);
        if (createdByUserId == Guid.Empty)
            return Result.Failure<DriveFileVersion>(DomainErrors.Drive.PrincipalIdEmpty);

        var version = new DriveFileVersion
        {
            VersionId = Guid.NewGuid(),
            FileItemId = fileItemId,
            VersionNumber = versionNumber,
            StorageProviderId = storageProviderId,
            ObjectKey = objectKey.Trim(),
            SizeBytes = declaredSizeBytes,
            MimeType = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType.Trim(),
            SourceId = sourceId,
            StatusId = DriveVersionStatus.PendingId,
            CreatedByUserId = createdByUserId,
            CreatedAt = utcNow ?? DateTime.UtcNow
        };
        return Result.Success(version);
    }

    /// <summary>Commits the version after the stored object's size and checksum were verified.</summary>
    public Result Commit(string? checksumSha256, long actualSizeBytes, DateTime? utcNow = null)
    {
        if (!IsPending)
            return Result.Failure(DomainErrors.Drive.VersionNotPending);
        if (string.IsNullOrWhiteSpace(checksumSha256) || checksumSha256.Trim().Length != 64)
            return Result.Failure(DomainErrors.Drive.ChecksumInvalid);
        if (actualSizeBytes <= 0)
            return Result.Failure(DomainErrors.Drive.VersionSizeInvalid);

        ChecksumSha256 = checksumSha256.Trim().ToLowerInvariant();
        SizeBytes = actualSizeBytes;
        StatusId = DriveVersionStatus.CommittedId;
        CommittedAt = utcNow ?? DateTime.UtcNow;
        return Result.Success();
    }

    public Result Abandon()
    {
        if (!IsPending)
            return Result.Failure(DomainErrors.Drive.VersionNotPending);

        StatusId = DriveVersionStatus.AbandonedId;
        return Result.Success();
    }
}