using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Infrastructure.Storage;

/// <summary>
/// Checks an upload against the selected <see cref="DocumentFileType"/> before it is persisted:
/// extension allow-list, file-type size limit, and the magic bytes of the real format. A renamed
/// executable therefore cannot be stored as a PDF or an image.
/// </summary>
public sealed class FileSignatureValidator : IFileSignatureValidator
{
    private static readonly byte[] Pdf = "%PDF-"u8.ToArray();
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] TiffLittleEndian = [0x49, 0x49, 0x2A, 0x00];
    private static readonly byte[] TiffBigEndian = [0x4D, 0x4D, 0x00, 0x2A];
    private static readonly byte[] Zip = [0x50, 0x4B, 0x03, 0x04];
    private static readonly byte[] OleCompound = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    public Result Validate(
        ReadOnlySpan<byte> header,
        string originalFileName,
        long fileSizeBytes,
        DocumentFileType fileType)
    {
        var extension = Path.GetExtension(originalFileName).ToLowerInvariant();

        if (fileSizeBytes <= 0)
            return Result.Failure(DomainErrors.Attachments.FileEmpty);

        if (fileSizeBytes > fileType.MaxFileSizeBytes)
            return Result.Failure(DomainErrors.Attachments.FileTooLarge);

        var allowed = fileType.AllowedExtensionsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => value.StartsWith('.') ? value : "." + value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(extension) || !allowed.Contains(extension))
            return Result.Failure(DomainErrors.Attachments.ExtensionNotAllowed);

        return extension switch
        {
            ".pdf" => StartsWith(header, Pdf) ? Result.Success() : Mismatch(),
            ".png" => StartsWith(header, Png) ? Result.Success() : Mismatch(),
            ".jpg" or ".jpeg" => StartsWith(header, Jpeg) ? Result.Success() : Mismatch(),
            ".tif" or ".tiff" => StartsWith(header, TiffLittleEndian) || StartsWith(header, TiffBigEndian)
                ? Result.Success()
                : Mismatch(),
            ".docx" or ".xlsx" or ".pptx" or ".zip" => StartsWith(header, Zip) ? Result.Success() : Mismatch(),
            ".doc" or ".xls" or ".ppt" => StartsWith(header, OleCompound) ? Result.Success() : Mismatch(),
            ".svg" => IsSvg(header) ? Result.Success() : Mismatch(),
            // Unknown-but-allowed extensions have no signature to check; the allow-list still applies.
            _ => Result.Success()
        };
    }

    private static Result Mismatch() => Result.Failure(DomainErrors.Attachments.SignatureMismatch);

    private static bool StartsWith(ReadOnlySpan<byte> header, byte[] signature) =>
        header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature);

    private static bool IsSvg(ReadOnlySpan<byte> header)
    {
        var text = System.Text.Encoding.UTF8.GetString(header).TrimStart();
        return text.StartsWith("<svg", StringComparison.OrdinalIgnoreCase) ||
               text.StartsWith("<?xml", StringComparison.OrdinalIgnoreCase);
    }
}
