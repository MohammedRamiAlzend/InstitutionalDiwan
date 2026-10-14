using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Domain.Entities.Attachments;

public sealed class FileStorageProvider : Entity<string>
{
    public string Code
    {
        get => Id;
        private set => Id = value;
    }

    public string NameAr { get; private set; } = null!;
    public string ProviderType { get; private set; } = "LOCAL_FS";
    public string BasePathOrEndpoint { get; private set; } = null!;
    public bool IsDefault { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private FileStorageProvider()
    {
    }

    public static Result<FileStorageProvider> Create(
        string? code,
        string? nameAr,
        string? basePathOrEndpoint,
        string providerType = "LOCAL_FS",
        bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(nameAr) || string.IsNullOrWhiteSpace(basePathOrEndpoint))
            return Result.Failure<FileStorageProvider>(DomainErrors.Attachments.StoragePathEmpty);

        var provider = new FileStorageProvider
        {
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            ProviderType = providerType.Trim().ToUpperInvariant(),
            BasePathOrEndpoint = basePathOrEndpoint.Trim(),
            IsDefault = isDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(provider);
    }
}
