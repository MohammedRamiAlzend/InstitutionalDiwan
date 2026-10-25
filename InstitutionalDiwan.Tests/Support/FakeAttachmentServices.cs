using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

internal sealed class FakeDocumentAccessEvaluator : IDocumentAccessEvaluator
{
    /// <summary>Documents the caller may reach at MANAGE (which also covers VIEW/DOWNLOAD).</summary>
    public HashSet<Guid> ManagedDocuments { get; } = new();

    /// <summary>Documents the caller may reach at VIEW only.</summary>
    public HashSet<Guid> ViewableDocuments { get; } = new();

    public Task<bool> CanAccessDocumentAsync(
        Guid documentId,
        int requiredAccessLevelId,
        CancellationToken cancellationToken = default)
    {
        if (ManagedDocuments.Contains(documentId))
            return Task.FromResult(true);

        if (requiredAccessLevelId <= 1 && ViewableDocuments.Contains(documentId))
            return Task.FromResult(true);

        return Task.FromResult(false);
    }
}

internal sealed class FakeDocumentFileRepository : IDocumentFileRepository
{
    public List<DocumentFile> Files { get; } = new();

    public Task<DocumentFile?> GetByIdAsync(Guid fileId, CancellationToken cancellationToken = default)
        => Task.FromResult(Files.FirstOrDefault(file => file.FileId == fileId));

    public Task<IReadOnlyList<DocumentFile>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DocumentFile>>(
            Files.Where(file => file.DocumentId == documentId)
                .OrderBy(file => file.CreatedAt)
                .ToList());

    public void Add(DocumentFile documentFile) => Files.Add(documentFile);
}

internal sealed class FakeDocumentFileTypeRepository : IDocumentFileTypeRepository
{
    public List<DocumentFileType> FileTypes { get; } = new();

    public Task<DocumentFileType?> GetByIdAsync(
        int fileTypeId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(FileTypes.FirstOrDefault(type => type.Id == fileTypeId));

    public FakeDocumentFileTypeRepository WithDefaultTypes()
    {
        FileTypes.Add(DocumentFileType.Create(
            DocumentFileType.ScanId, "Scan", "مسح ضوئي", ".pdf,.png", 1024 * 1024).Value);
        FileTypes.Add(DocumentFileType.Create(
            DocumentFileType.AttachmentId, "Attachment", "مرفق", ".pdf,.docx", 1024 * 1024).Value);
        return this;
    }
}

internal sealed class FakeFileStorageProviderRepository : IFileStorageProviderRepository
{
    public List<FileStorageProvider> Providers { get; } = new();

    public Task<FileStorageProvider?> GetByIdAsync(
        int storageProviderId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Providers.FirstOrDefault(provider => provider.Id == storageProviderId));

    public Task<FileStorageProvider?> GetDefaultAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Providers.FirstOrDefault(provider => provider.IsDefault && provider.IsActive)
                           ?? Providers.FirstOrDefault(provider => provider.IsActive));

    public FakeFileStorageProviderRepository WithLocalProvider(string root = "storage/documents")
    {
        Providers.Add(FileStorageProvider.Create(
            FileStorageProvider.LocalDefaultId,
            "Local",
            "محلي",
            root,
            StorageProviderType.LocalFileSystemId,
            isDefault: true).Value);
        return this;
    }
}

internal sealed class FakeFileSignatureValidator : IFileSignatureValidator
{
    public Error? Failure { get; set; }

    public Result Validate(
        ReadOnlySpan<byte> header,
        string originalFileName,
        long fileSizeBytes,
        DocumentFileType fileType)
        => Failure is null ? Result.Success() : Result.Failure(Failure);
}

internal sealed class FakeFileStorageService : IFileStorageService
{
    public List<(Guid DocumentId, string OriginalFileName, long Size)> Saved { get; } = new();
    public Dictionary<string, byte[]> StoredContent { get; } = new(StringComparer.OrdinalIgnoreCase);
    public string Checksum { get; set; } = new('a', 64);

    public async Task<StoredFile> SaveAsync(
        Stream content,
        string originalFileName,
        string? mimeType,
        Guid documentId,
        int storageProviderId,
        string basePathOrEndpoint,
        CancellationToken cancellationToken = default)
    {
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        var relativePath = $"{documentId:D}/version.bin";
        StoredContent[relativePath] = buffer.ToArray();
        Saved.Add((documentId, originalFileName, buffer.Length));

        return new StoredFile(
            relativePath,
            originalFileName,
            mimeType ?? "application/pdf",
            buffer.Length,
            Checksum,
            storageProviderId);
    }

    public Task<Stream?> OpenReadAsync(
        string basePathOrEndpoint,
        string relativePath,
        CancellationToken cancellationToken = default)
        => Task.FromResult<Stream?>(
            StoredContent.TryGetValue(relativePath, out var bytes)
                ? new MemoryStream(bytes, writable: false)
                : null);

    public async Task<string> ComputeSha256Async(
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var hash = await System.Security.Cryptography.SHA256.HashDataAsync(content, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
