using System.Security.Cryptography;
using InstitutionalDiwan.Application.Abstractions.Storage;

namespace InstitutionalDiwan.Infrastructure.Storage;

/// <summary>
/// Stores document files on the local file system (or a mounted network share) under
/// <c>{Year}/{Month}/{DocumentId}/{Random}.ext</c> and computes the SHA-256 checksum while the
/// bytes are written, so the content is hashed exactly once.
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private const int BufferSize = 81920;

    public async Task<StoredFile> SaveAsync(
        Stream content,
        string originalFileName,
        string? mimeType,
        Guid documentId,
        int storageProviderId,
        string basePathOrEndpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(basePathOrEndpoint);

        var root = ResolveRoot(basePathOrEndpoint);
        var now = DateTime.UtcNow;
        var extension = Path.GetExtension(originalFileName);
        var relativeDirectory = Path.Combine(
            now.Year.ToString("0000"),
            now.Month.ToString("00"),
            documentId.ToString("D"));
        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var relativePath = Path.Combine(relativeDirectory, fileName);

        Directory.CreateDirectory(Path.Combine(root, relativeDirectory));

        var fullPath = ResolveUnderRoot(root, relativePath);
        long size = 0;

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        await using (var output = new FileStream(
                         fullPath,
                         FileMode.CreateNew,
                         FileAccess.Write,
                         FileShare.None,
                         BufferSize,
                         FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            var buffer = new byte[BufferSize];
            int read;
            while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
            {
                hash.AppendData(buffer, 0, read);
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                size += read;
            }
        }

        var checksum = Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant();

        return new StoredFile(
            relativePath.Replace('\\', '/'),
            Path.GetFileName(originalFileName),
            string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType.Trim(),
            size,
            checksum,
            storageProviderId);
    }

    public Task<Stream?> OpenReadAsync(
        string basePathOrEndpoint,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(basePathOrEndpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);

        var root = ResolveRoot(basePathOrEndpoint);
        var fullPath = ResolveUnderRoot(root, relativePath);

        if (!File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        return Task.FromResult<Stream?>(stream);
    }

    public async Task<string> ComputeSha256Async(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var hash = await SHA256.HashDataAsync(content, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    /// <summary>
    /// Resolves a configured root. Relative roots are anchored at the application directory so a
    /// seeded value such as <c>storage/documents</c> works without extra configuration.
    /// </summary>
    private static string ResolveRoot(string basePathOrEndpoint) =>
        Path.GetFullPath(basePathOrEndpoint, AppContext.BaseDirectory);

    /// <summary>
    /// Combines a relative path with the root and refuses anything that would escape the root, so a
    /// tampered database row cannot read or write arbitrary files.
    /// </summary>
    private static string ResolveUnderRoot(string root, string relativePath)
    {
        var fullPath = Path.GetFullPath(Path.Combine(root, relativePath));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar)
            ? root
            : root + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("The stored file path escapes the storage root.");

        return fullPath;
    }
}
