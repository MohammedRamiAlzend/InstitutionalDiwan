namespace InstitutionalDiwan.Application.Abstractions.Storage;

/// <summary>A short-lived presigned upload target in the S3-compatible store.</summary>
public sealed record DrivePresignedUpload(Uri Url, DateTime ExpiresAtUtc);

/// <summary>Metadata of a stored object, read without downloading its content.</summary>
public sealed record DriveStoredObject(long SizeBytes, string? ContentType);

/// <summary>
/// Object storage for the Drive module (Module 11), implemented against the S3-compatible
/// SeaweedFS gateway. Unlike <see cref="IFileStorageService"/> (document attachments, local
/// paths), Drive streams bytes directly between the browser and the store via presigned URLs;
/// this service only signs, verifies, ranges and deletes. The provider endpoint and credentials
/// come from configuration, never from callers.
/// </summary>
public interface IDriveStorageService
{
    /// <summary>Signs a direct PUT upload for the object key, bounded by size and lifetime.</summary>
    Task<DrivePresignedUpload> CreateUploadUrlAsync(
        string objectKey,
        string? mimeType,
        long maxBytes,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    /// <summary>Signs a short-lived GET download URL reachable by the browser and by OnlyOffice.</summary>
    Task<Uri> CreateDownloadUrlAsync(
        string objectKey,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default);

    /// <summary>Reads object metadata, or null when the object does not exist.</summary>
    Task<DriveStoredObject?> HeadAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens the object for reading, optionally a single inclusive byte range (HTTP Range
    /// streaming for video). Returns null when the object does not exist.
    /// </summary>
    Task<Stream?> OpenReadAsync(
        string objectKey,
        long? rangeStart = null,
        long? rangeEnd = null,
        CancellationToken cancellationToken = default);

    /// <summary>Computes the lower-case 64-character hex SHA-256 checksum of a readable stream.</summary>
    Task<string> ComputeSha256Async(Stream content, CancellationToken cancellationToken = default);

    /// <summary>Deletes the object; succeeds silently when it is already gone.</summary>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default);

    /// <summary>Server-side copies an object to a new key (restoring an old version as a new one).</summary>
    Task CopyAsync(string sourceObjectKey, string destinationObjectKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a stream to a new object key (the OnlyOffice save callback stores the edited bytes
    /// server-side). The stream is read from its current position; callers pass a rewound stream.
    /// </summary>
    Task WriteAsync(string objectKey, Stream content, string? mimeType, CancellationToken cancellationToken = default);
}