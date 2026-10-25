using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;

namespace InstitutionalDiwan.Application.Abstractions.Storage;

/// <summary>
/// Validates an upload before anything is written: the extension must be allowed by the file type,
/// the size must be positive and within the type's limit, and the magic bytes must match the file's
/// real format (so a renamed executable cannot masquerade as a PDF or an image).
/// </summary>
public interface IFileSignatureValidator
{
    /// <summary>
    /// Validates a file using its name, size, and first bytes (at least 16 bytes when available).
    /// Returns the matching domain error on failure and an empty <see cref="Result"/> on success.
    /// </summary>
    Result Validate(
        ReadOnlySpan<byte> header,
        string originalFileName,
        long fileSizeBytes,
        DocumentFileType fileType);
}
