using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>
/// Drive upload limits (Module 11): the institution allow-list and max file size from
/// <see cref="InstitutionSetting"/>. The pre-byte check runs at initiate (extension + declared size);
/// the magic-byte check runs at complete, reusing <see cref="IFileSignatureValidator"/> against a
/// transient file type carrying the Drive limits, with the errors mapped to the Drive group.
/// </summary>
internal sealed class DriveUploadGuard
{
    private readonly IFileSignatureValidator _signatureValidator;

    public DriveUploadGuard(IFileSignatureValidator signatureValidator) =>
        _signatureValidator = signatureValidator;

    /// <summary>Validates the declared name/size against the Drive limits before any bytes are reserved.</summary>
    public Result CheckDeclared(string fileName, long declaredSizeBytes, InstitutionSetting setting)
    {
        if (declaredSizeBytes <= 0)
            return Result.Failure(DomainErrors.Drive.VersionSizeInvalid);
        if (declaredSizeBytes > setting.DriveMaxFileSizeBytes)
            return Result.Failure(DomainErrors.Drive.FileTooLarge);
        if (!IsExtensionAllowed(Path.GetExtension(fileName), setting.DriveAllowedExtensionsCsv))
            return Result.Failure(DomainErrors.Drive.ExtensionNotAllowed);
        return Result.Success();
    }

    /// <summary>Validates the stored object's magic bytes against its extension at complete time.</summary>
    public Result CheckSignature(ReadOnlySpan<byte> header, string fileName, long actualSizeBytes, InstitutionSetting setting)
    {
        var fileType = DocumentFileType.Create(
            1, "Drive", "ملفات", setting.DriveAllowedExtensionsCsv, setting.DriveMaxFileSizeBytes).Value;
        var result = _signatureValidator.Validate(header, fileName, actualSizeBytes, fileType);
        return result.IsSuccess ? Result.Success() : Result.Failure(MapError(result.Error));
    }

    public static bool IsExtensionAllowed(string? extension, string allowedExtensionsCsv)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return false;
        var normalized = extension.StartsWith('.') ? extension.ToLowerInvariant() : "." + extension.ToLowerInvariant();
        return allowedExtensionsCsv
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(entry => entry.StartsWith('.') ? entry.ToLowerInvariant() : "." + entry.ToLowerInvariant())
            .Contains(normalized);
    }

    private static Error MapError(Error error) => error.Code switch
    {
        "Attachments.FileTooLarge" => DomainErrors.Drive.FileTooLarge,
        "Attachments.ExtensionNotAllowed" => DomainErrors.Drive.ExtensionNotAllowed,
        "Attachments.SignatureMismatch" => DomainErrors.Drive.SignatureMismatch,
        _ => DomainErrors.Drive.SignatureMismatch
    };
}