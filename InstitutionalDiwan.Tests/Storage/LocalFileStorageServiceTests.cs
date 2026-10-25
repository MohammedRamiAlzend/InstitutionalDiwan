using System.Security.Cryptography;
using System.Text;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Infrastructure.Storage;

namespace InstitutionalDiwan.Tests.Storage;

public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _root = Path.Combine(
        Path.GetTempPath(), "InstitutionalDiwan.Tests", Guid.NewGuid().ToString("N"));

    private readonly LocalFileStorageService _storage = new();

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private static byte[] Payload => "%PDF-1.7 streaming checksum payload"u8.ToArray();

    [Fact]
    public async Task SaveAsync_WritesUnderYearMonthDocumentAndReturnsStreamedChecksum()
    {
        await using var content = new MemoryStream(Payload);

        var stored = await _storage.SaveAsync(
            content,
            "Scan.PDF",
            "application/pdf",
            Guid.Parse("7c9e3b42-8a61-4d52-9f17-2ab85c6e7934"),
            FileStorageProvider.LocalDefaultId,
            _root);

        var expected = Convert.ToHexString(SHA256.HashData(Payload)).ToLowerInvariant();
        Assert.Equal(expected, stored.ChecksumSha256);
        Assert.Equal(Payload.Length, stored.FileSizeBytes);
        Assert.Equal("Scan.PDF", stored.OriginalFileName);
        Assert.Equal("application/pdf", stored.MimeType);
        Assert.Equal(FileStorageProvider.LocalDefaultId, stored.StorageProviderId);
        Assert.Equal(".pdf", Path.GetExtension(stored.RelativePath));
        Assert.Contains("7c9e3b42-8a61-4d52-9f17-2ab85c6e7934", stored.RelativePath);

        var fullPath = Path.Combine(_root, stored.RelativePath);
        Assert.True(File.Exists(fullPath));
    }

    [Fact]
    public async Task OpenReadAsync_ReturnsTheStoredBytes()
    {
        await using var content = new MemoryStream(Payload);
        var stored = await _storage.SaveAsync(
            content, "scan.pdf", "application/pdf", Guid.NewGuid(),
            FileStorageProvider.LocalDefaultId, _root);

        await using var stream = await _storage.OpenReadAsync(_root, stored.RelativePath);

        Assert.NotNull(stream);
        await using var buffer = new MemoryStream();
        await stream!.CopyToAsync(buffer);
        Assert.Equal(Payload, buffer.ToArray());
    }

    [Fact]
    public async Task OpenReadAsync_ReturnsNullWhenTheFileIsGone()
    {
        var stream = await _storage.OpenReadAsync(_root, "2026/01/missing/file.pdf");

        Assert.Null(stream);
    }

    [Fact]
    public async Task OpenReadAsync_RefusesAPathThatEscapesTheRoot()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _storage.OpenReadAsync(_root, "../outside.pdf"));
    }

    [Fact]
    public async Task ComputeSha256Async_ReturnsSixtyFourLowercaseHexCharacters()
    {
        await using var content = new MemoryStream(Encoding.UTF8.GetBytes("abc"));

        var checksum = await _storage.ComputeSha256Async(content);

        Assert.Equal(
            "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad",
            checksum);
    }
}
