namespace InstitutionalDiwan.Domain.Entities.Attachments;

/// <summary>
/// Reference table of configured file storage providers. Replaces the former string
/// <c>DocumentFileVersion.StorageProviderCode</c>; versions now reference it by id. Seeded by
/// <c>FileStorageProviderSeeder</c>, which always provides a default local-file-system provider.
/// </summary>
public sealed class FileStorageProvider : LookupEntity
{
    public const int LocalDefaultId = 1;

    /// <summary>FK → StorageProviderType.Id.</summary>
    public int ProviderTypeId { get; private set; } = StorageProviderType.LocalFileSystemId;

    /// <summary>Root path (local/network) or endpoint URL (cloud) used to store files.</summary>
    public string BasePathOrEndpoint { get; private set; } = null!;

    public bool IsDefault { get; private set; }

    private FileStorageProvider()
    {
    }

    private FileStorageProvider(int id, string desc, string descAr, int providerTypeId, string basePathOrEndpoint, bool isDefault)
        : base(id, desc, descAr)
    {
        ProviderTypeId = providerTypeId;
        BasePathOrEndpoint = basePathOrEndpoint.Trim();
        IsDefault = isDefault;
    }

    public static Result<FileStorageProvider> Create(
        int id,
        string? desc,
        string? descAr,
        string? basePathOrEndpoint,
        int providerTypeId = StorageProviderType.LocalFileSystemId,
        bool isDefault = false)
    {
        if (id <= 0)
            return Result.Failure<FileStorageProvider>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<FileStorageProvider>(DomainErrors.Attachments.ProviderNameEmpty);

        if (string.IsNullOrWhiteSpace(basePathOrEndpoint))
            return Result.Failure<FileStorageProvider>(DomainErrors.Attachments.ProviderPathEmpty);

        if (providerTypeId <= 0)
            return Result.Failure<FileStorageProvider>(DomainErrors.Lookup.InvalidId);

        return Result.Success(new FileStorageProvider(id, desc, descAr, providerTypeId, basePathOrEndpoint, isDefault));
    }
}
