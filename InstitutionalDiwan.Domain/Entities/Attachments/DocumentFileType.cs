namespace InstitutionalDiwan.Domain.Entities.Attachments;

/// <summary>
/// Reference table of document file types. Replaces the former string <c>DocumentFile.FileTypeCode</c>;
/// files now reference it by id. Seeded by <c>DocumentFileTypeSeeder</c>.
/// </summary>
public sealed class DocumentFileType : LookupEntity
{
    public const int ScanId = 1;
    public const int AttachmentId = 2;
    public const int SignatureId = 3;
    public const int OtherId = 4;

    /// <summary>Comma-separated list of extensions accepted for this file type.</summary>
    public string AllowedExtensionsCsv { get; private set; } = ".pdf,.tif,.tiff,.png,.jpg";

    /// <summary>Maximum accepted file size in bytes.</summary>
    public long MaxFileSizeBytes { get; private set; } = 52428800;

    private DocumentFileType()
    {
    }

    private DocumentFileType(int id, string desc, string descAr, string allowedExtensionsCsv, long maxFileSizeBytes)
        : base(id, desc, descAr)
    {
        AllowedExtensionsCsv = allowedExtensionsCsv.Trim();
        MaxFileSizeBytes = maxFileSizeBytes;
    }

    public static Result<DocumentFileType> Create(
        int id,
        string? desc,
        string? descAr,
        string allowedExtensionsCsv = ".pdf,.tif,.tiff,.png,.jpg",
        long maxFileSizeBytes = 52428800)
    {
        if (id <= 0)
            return Result.Failure<DocumentFileType>(DomainErrors.Lookup.InvalidId);

        if (string.IsNullOrWhiteSpace(desc) || string.IsNullOrWhiteSpace(descAr))
            return Result.Failure<DocumentFileType>(DomainErrors.Attachments.FileTypeEmpty);

        if (string.IsNullOrWhiteSpace(allowedExtensionsCsv) || maxFileSizeBytes <= 0)
            return Result.Failure<DocumentFileType>(DomainErrors.Attachments.FileTypeEmpty);

        return Result.Success(new DocumentFileType(id, desc, descAr, allowedExtensionsCsv, maxFileSizeBytes));
    }
}
