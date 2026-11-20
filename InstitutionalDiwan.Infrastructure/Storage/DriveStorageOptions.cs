namespace InstitutionalDiwan.Infrastructure.Storage;

/// <summary>
/// Connection settings for the Drive S3-compatible store (SeaweedFS). Endpoint and bucket are
/// non-secret; AccessKey/SecretKey belong in user-secrets or environment variables, never in
/// committed appsettings. The endpoint must be reachable from the server, the browser and the
/// OnlyOffice server, because presigned URLs are signed against this exact host.
/// </summary>
public sealed class DriveStorageOptions
{
    public const string SectionName = "Drive:Storage";

    public string Endpoint { get; set; } = string.Empty;
    public string Bucket { get; set; } = "drive";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string Region { get; set; } = "us-east-1";

    /// <summary>SeaweedFS (and most S3-compatible gateways) require path-style addressing.</summary>
    public bool ForcePathStyle { get; set; } = true;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Endpoint) &&
        !string.IsNullOrWhiteSpace(Bucket) &&
        !string.IsNullOrWhiteSpace(AccessKey) &&
        !string.IsNullOrWhiteSpace(SecretKey);
}