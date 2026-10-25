namespace InstitutionalDiwan.Application.Abstractions.Storage;

/// <summary>
/// Physical storage for document scans and attachments (Module 06). Implementations persist the
/// bytes under a provider-specific root, verify them while streaming, and return the SHA-256
/// checksum the domain records on <c>DocumentFileVersion</c>.
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Streams <paramref name="content"/> to storage under
    /// <c>{Year}/{Month}/{DocumentId}/{Random}.ext</c>, computing the SHA-256 checksum in the same
    /// pass. The caller supplies the provider's root path and id.
    /// </summary>
    Task<StoredFile> SaveAsync(
        Stream content,
        string originalFileName,
        string? mimeType,
        Guid documentId,
        int storageProviderId,
        string basePathOrEndpoint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a previously stored file for reading, or returns <c>null</c> when the file is no longer
    /// present on disk. <paramref name="relativePath"/> is resolved under
    /// <paramref name="basePathOrEndpoint"/> and may never escape it.
    /// </summary>
    Task<Stream?> OpenReadAsync(
        string basePathOrEndpoint,
        string relativePath,
        CancellationToken cancellationToken = default);

    /// <summary>Computes the lower-case 64-character hex SHA-256 checksum of a readable stream.</summary>
    Task<string> ComputeSha256Async(Stream content, CancellationToken cancellationToken = default);
}
