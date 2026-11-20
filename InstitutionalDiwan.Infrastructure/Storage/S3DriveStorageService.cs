using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using InstitutionalDiwan.Application.Abstractions.Storage;
using Microsoft.Extensions.Options;
using System.Net;
using System.Security.Cryptography;

namespace InstitutionalDiwan.Infrastructure.Storage;

/// <summary>
/// Drive object storage (Module 11) against the S3-compatible SeaweedFS gateway. Bytes never pass
/// through this service on upload: the browser PUTs directly to a presigned URL. A presigned PUT
/// cannot carry a size condition, so <c>maxBytes</c> is enforced at commit time via HEAD, not at
/// signing time. Reads support a single inclusive byte range for video streaming.
/// </summary>
public sealed class S3DriveStorageService : IDriveStorageService, IDisposable
{
    private readonly DriveStorageOptions _options;
    private readonly Lazy<AmazonS3Client> _client;

    public S3DriveStorageService(IOptions<DriveStorageOptions> options)
    {
        _options = options.Value;
        _client = new Lazy<AmazonS3Client>(CreateClient);
    }

    public Task<DrivePresignedUpload> CreateUploadUrlAsync(
        string objectKey,
        string? mimeType,
        long maxBytes,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(objectKey);
        if (maxBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxBytes));
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime));

        var expiresAt = DateTime.UtcNow.Add(lifetime);
        var url = Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            Verb = HttpVerb.PUT,
            Expires = expiresAt,
            ContentType = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType.Trim()
        });

        return Task.FromResult(new DrivePresignedUpload(new Uri(url), expiresAt));
    }

    public Task<Uri> CreateDownloadUrlAsync(
        string objectKey,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(objectKey);
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime));

        var url = Client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime)
        });

        return Task.FromResult(new Uri(url));
    }

    public async Task<DriveStoredObject?> HeadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        ValidateKey(objectKey);
        try
        {
            var metadata = await Client.GetObjectMetadataAsync(_options.Bucket, objectKey, cancellationToken);
            return new DriveStoredObject(metadata.ContentLength, metadata.Headers.ContentType);
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<Stream?> OpenReadAsync(
        string objectKey,
        long? rangeStart = null,
        long? rangeEnd = null,
        CancellationToken cancellationToken = default)
    {
        ValidateKey(objectKey);
        if (rangeStart < 0 || rangeEnd < 0 || (rangeStart.HasValue && rangeEnd.HasValue && rangeEnd < rangeStart))
            throw new ArgumentOutOfRangeException(nameof(rangeStart), "The byte range is invalid.");

        var request = new GetObjectRequest { BucketName = _options.Bucket, Key = objectKey };
        if (rangeStart.HasValue || rangeEnd.HasValue)
            request.ByteRange = new ByteRange(rangeStart ?? 0, rangeEnd ?? long.MaxValue);

        try
        {
            var response = await Client.GetObjectAsync(request, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception exception) when (exception.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<string> ComputeSha256Async(Stream content, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        var hash = await SHA256.HashDataAsync(content, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public async Task DeleteAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        ValidateKey(objectKey);
        // S3 DELETE is idempotent: a missing object is a success.
        await Client.DeleteObjectAsync(_options.Bucket, objectKey, cancellationToken);
    }

    public async Task CopyAsync(string sourceObjectKey, string destinationObjectKey, CancellationToken cancellationToken = default)
    {
        ValidateKey(sourceObjectKey);
        ValidateKey(destinationObjectKey);
        await Client.CopyObjectAsync(new CopyObjectRequest
        {
            SourceBucket = _options.Bucket,
            SourceKey = sourceObjectKey,
            DestinationBucket = _options.Bucket,
            DestinationKey = destinationObjectKey
        }, cancellationToken);
    }

    public async Task WriteAsync(string objectKey, Stream content, string? mimeType, CancellationToken cancellationToken = default)
    {
        ValidateKey(objectKey);
        ArgumentNullException.ThrowIfNull(content);
        if (content.CanSeek)
            content.Position = 0;

        await Client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.Bucket,
            Key = objectKey,
            InputStream = content,
            ContentType = string.IsNullOrWhiteSpace(mimeType) ? "application/octet-stream" : mimeType.Trim(),
            AutoCloseStream = false
        }, cancellationToken);
    }

    public void Dispose()
    {
        if (_client.IsValueCreated)
            _client.Value.Dispose();
    }

    private AmazonS3Client CreateClient()
    {
        if (!_options.IsConfigured)
            throw new InvalidOperationException(
                "Drive storage is not configured. Set Drive:Storage Endpoint, Bucket, AccessKey and SecretKey (user-secrets).");

        var config = new AmazonS3Config
        {
            ServiceURL = _options.Endpoint.Trim(),
            ForcePathStyle = _options.ForcePathStyle,
            AuthenticationRegion = string.IsNullOrWhiteSpace(_options.Region) ? "us-east-1" : _options.Region.Trim()
        };

        return new AmazonS3Client(new BasicAWSCredentials(_options.AccessKey, _options.SecretKey), config);
    }

    private static void ValidateKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            throw new ArgumentException("An object key is required.", nameof(objectKey));
        if (objectKey.StartsWith('/') || objectKey.Contains('\\') || objectKey.Split('/').Contains(".."))
            throw new ArgumentException("The object key must be a relative, normalized S3 key.", nameof(objectKey));
    }

    private AmazonS3Client Client => _client.Value;
}