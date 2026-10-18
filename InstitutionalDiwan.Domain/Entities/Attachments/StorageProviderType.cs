using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Attachments;

/// <summary>
/// Reference table classifying a file storage provider backend. Replaces the former string
/// <c>FileStorageProvider.ProviderType</c>. Seeded by <c>StorageProviderTypeSeeder</c>.
/// </summary>
public sealed class StorageProviderType : LookupEntity
{
    public const int LocalFileSystemId = 1;
    public const int NetworkShareId = 2;
    public const int AzureBlobId = 3;
    public const int AwsS3Id = 4;

    private StorageProviderType()
    {
    }

    private StorageProviderType(int id, string desc, string descAr) : base(id, desc, descAr)
    {
    }

    public static Result<StorageProviderType> Create(int id, string? desc, string? descAr)
    {
        if (id <= 0)
            return Result.Failure<StorageProviderType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<StorageProviderType>(DomainErrors.Lookup.LabelEmpty);

        return Result.Success(new StorageProviderType(id, desc, descAr));
    }
}
