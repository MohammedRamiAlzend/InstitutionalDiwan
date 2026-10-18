using InstitutionalDiwan.Domain.Entities.Attachments;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Seeds the storage provider type lookup (Order 52, every environment).</summary>
public sealed class StorageProviderTypeSeeder : LookupSeederBase<StorageProviderType>
{
    public override int Order => 52;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (StorageProviderType.LocalFileSystemId, "Local file system", "نظام ملفات محلي"),
        (StorageProviderType.NetworkShareId, "Network share", "مشاركة شبكية"),
        (StorageProviderType.AzureBlobId, "Azure Blob Storage", "تخزين Azure Blob"),
        (StorageProviderType.AwsS3Id, "Amazon S3", "أمازون S3"),
    };

    protected override StorageProviderType BuildItem(int id, string desc, string descAr) =>
        StorageProviderType.Create(id, desc, descAr).Value;
}

/// <summary>Seeds the document file type lookup (Order 53, every environment).</summary>
public sealed class DocumentFileTypeSeeder : LookupSeederBase<DocumentFileType>
{
    public override int Order => 53;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (DocumentFileType.ScanId, "Scan", "مسح ضوئي"),
        (DocumentFileType.AttachmentId, "Attachment", "مرفق"),
        (DocumentFileType.SignatureId, "Signature", "توقيع"),
        (DocumentFileType.OtherId, "Other", "أخرى"),
    };

    protected override DocumentFileType BuildItem(int id, string desc, string descAr)
    {
        var (extensions, maxBytes) = id switch
        {
            DocumentFileType.ScanId => (".pdf,.jpg,.jpeg,.png,.tif,.tiff", 20L * 1024 * 1024),
            DocumentFileType.SignatureId => (".png,.jpg,.jpeg,.svg", 2L * 1024 * 1024),
            _ => (".pdf,.doc,.docx,.xls,.xlsx,.jpg,.jpeg,.png,.tif,.tiff,.zip", 50L * 1024 * 1024),
        };

        return DocumentFileType.Create(id, desc, descAr, extensions, maxBytes).Value;
    }
}

/// <summary>Seeds the file storage provider lookup (Order 54, every environment).</summary>
public sealed class FileStorageProviderSeeder : LookupSeederBase<FileStorageProvider>
{
    public override int Order => 54;

    protected override IReadOnlyList<(int Id, string Desc, string DescAr)> Catalog { get; } = new (int, string, string)[]
    {
        (FileStorageProvider.LocalDefaultId, "Default local storage", "التخزين المحلي الافتراضي"),
    };

    protected override FileStorageProvider BuildItem(int id, string desc, string descAr) =>
        FileStorageProvider.Create(
            id,
            desc,
            descAr,
            "storage/documents",
            StorageProviderType.LocalFileSystemId,
            isDefault: true).Value;
}
