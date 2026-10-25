namespace InstitutionalDiwan.Application.Abstractions.Storage;

/// <summary>
/// A file that has been written to physical storage. <see cref="ChecksumSha256"/> is computed while
/// the bytes stream to their destination, so it can be handed straight to
/// <c>DocumentFile.AddVersion(...)</c> without reading the file a second time.
/// </summary>
public sealed record StoredFile(
    string RelativePath,
    string OriginalFileName,
    string MimeType,
    long FileSizeBytes,
    string ChecksumSha256,
    int StorageProviderId);
