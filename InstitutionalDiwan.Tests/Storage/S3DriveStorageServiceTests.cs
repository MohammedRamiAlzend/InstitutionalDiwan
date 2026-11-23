using InstitutionalDiwan.Infrastructure.Storage;
using Microsoft.Extensions.Options;

namespace InstitutionalDiwan.Tests.Storage;

/// <summary>
/// Offline checks for the S3-compatible Drive storage service: presigned URL shape, key
/// validation, the unconfigured guard and checksum computation. No live SeaweedFS is contacted.
/// </summary>
public sealed class S3DriveStorageServiceTests
{
    private static S3DriveStorageService CreateService() =>
        new(Options.Create(new DriveStorageOptions
        {
            Endpoint = "http://localhost:8333",
            Bucket = "drive",
            AccessKey = "unit-test-access",
            SecretKey = "unit-test-secret"
        }));

    [Fact]
    public async Task UploadUrl_IsPathStylePresignedPut_WithExpiry()
    {
        using var service = CreateService();
        var before = DateTime.UtcNow;

        var upload = await service.CreateUploadUrlAsync("space/file/1", "text/plain", 1024, TimeSpan.FromMinutes(15));

        Assert.Equal("localhost", upload.Url.Host);
        // AWSSDK.S3 4.x always presigns https unless the request asks for HTTP, which a plain-http
        // SeaweedFS gateway cannot answer (the browser reports ERR_SSL_PROTOCOL_ERROR).
        Assert.Equal(Uri.UriSchemeHttp, upload.Url.Scheme);
        Assert.Equal("/drive/space/file/1", upload.Url.AbsolutePath);
        Assert.Contains("X-Amz-Signature=", upload.Url.Query);
        Assert.Contains("X-Amz-Expires=900", upload.Url.Query);
        Assert.True(upload.ExpiresAtUtc >= before.AddMinutes(15));
    }

    [Fact]
    public async Task DownloadUrl_IsPathStylePresignedGet()
    {
        using var service = CreateService();

        var url = await service.CreateDownloadUrlAsync("space/file/1", TimeSpan.FromMinutes(5));

        Assert.Equal(Uri.UriSchemeHttp, url.Scheme);
        Assert.Equal("/drive/space/file/1", url.AbsolutePath);
        Assert.Contains("X-Amz-Signature=", url.Query);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("/leading-slash")]
    [InlineData("back\\slash")]
    [InlineData("space/../escape")]
    public async Task InvalidObjectKeys_Throw(string key)
    {
        using var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateUploadUrlAsync(key, null, 1024, TimeSpan.FromMinutes(5)));
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateDownloadUrlAsync(key, TimeSpan.FromMinutes(5)));
    }

    [Fact]
    public async Task InvalidSizeOrLifetime_Throw()
    {
        using var service = CreateService();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateUploadUrlAsync("space/file/1", null, 0, TimeSpan.FromMinutes(5)));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateUploadUrlAsync("space/file/1", null, 1024, TimeSpan.Zero));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.CreateDownloadUrlAsync("space/file/1", TimeSpan.Zero));
    }

    [Fact]
    public async Task Unconfigured_ThrowsWithGuidance()
    {
        using var service = new S3DriveStorageService(Options.Create(new DriveStorageOptions()));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateUploadUrlAsync("space/file/1", null, 1024, TimeSpan.FromMinutes(5)));
        Assert.Contains("user-secrets", exception.Message);
    }

    [Fact]
    public async Task Sha256_KnownVectors()
    {
        using var service = CreateService();

        var abc = await service.ComputeSha256Async(new MemoryStream("abc"u8.ToArray()));
        var empty = await service.ComputeSha256Async(new MemoryStream());

        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", abc);
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", empty);
    }
}