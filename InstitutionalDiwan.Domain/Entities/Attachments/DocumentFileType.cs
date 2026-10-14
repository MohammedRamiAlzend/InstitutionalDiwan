namespace InstitutionalDiwan.Domain.Entities.Attachments;

public sealed class DocumentFileType : Entity<string>
{
    public string Code
    {
        get => Id;
        private set => Id = value;
    }

    public string NameAr { get; private set; } = null!;
    public string? NameEn { get; private set; }
    public string AllowedExtensionsCsv { get; private set; } = ".pdf,.tif,.tiff,.png,.jpg";
    public long MaxFileSizeBytes { get; private set; } = 52428800;
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedAt { get; private set; }

    private DocumentFileType()
    {
    }

    public static Result<DocumentFileType> Create(
        string? code,
        string? nameAr,
        string allowedExtensionsCsv = ".pdf,.tif,.tiff,.png,.jpg",
        long maxFileSizeBytes = 52428800,
        string? nameEn = null)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Result.Failure<DocumentFileType>(DomainErrors.Attachments.FileTypeEmpty);

        if (string.IsNullOrWhiteSpace(nameAr))
            return Result.Failure<DocumentFileType>(DomainErrors.Attachments.TitleEmpty);

        var fileType = new DocumentFileType
        {
            Code = code.Trim().ToUpperInvariant(),
            NameAr = nameAr.Trim(),
            NameEn = string.IsNullOrWhiteSpace(nameEn) ? null : nameEn.Trim(),
            AllowedExtensionsCsv = allowedExtensionsCsv,
            MaxFileSizeBytes = maxFileSizeBytes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        return Result.Success(fileType);
    }
}
