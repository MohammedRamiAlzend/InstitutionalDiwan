using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Attachments;

/// <summary>
/// Shared upload pipeline for initial versions and later versions: size guard, one in-memory copy
/// for the magic-byte inspection, provider resolution, and storage while the SHA-256 is computed.
/// </summary>
internal sealed class AttachmentUploader
{
    private const int SignatureLength = 16;

    private readonly IFileStorageProviderRepository _providerRepository;
    private readonly IFileSignatureValidator _signatureValidator;
    private readonly IFileStorageService _storage;

    public AttachmentUploader(
        IFileStorageProviderRepository providerRepository,
        IFileSignatureValidator signatureValidator,
        IFileStorageService storage)
    {
        _providerRepository = providerRepository;
        _signatureValidator = signatureValidator;
        _storage = storage;
    }

    public async Task<Result<StoredFile>> StoreAsync(
        Guid documentId,
        DocumentFileType fileType,
        Stream content,
        string originalFileName,
        string? mimeType,
        long fileSizeBytes,
        CancellationToken cancellationToken)
    {
        // Reject an over-limit upload before buffering it; the validator repeats the check against
        // the bytes actually read in case the declared length lied.
        if (fileSizeBytes <= 0)
            return Result.Failure<StoredFile>(DomainErrors.Attachments.FileEmpty);

        if (fileSizeBytes > fileType.MaxFileSizeBytes)
            return Result.Failure<StoredFile>(DomainErrors.Attachments.FileTooLarge);

        // Buffer once so the validator can inspect the magic bytes and storage can still stream the
        // whole payload.
        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var header = buffer.GetBuffer().AsSpan(0, (int)Math.Min(buffer.Length, SignatureLength));
        var validation = _signatureValidator.Validate(
            header, originalFileName, buffer.Length, fileType);
        if (validation.IsFailure)
            return Result.Failure<StoredFile>(validation.Error);

        var provider = await _providerRepository.GetDefaultAsync(cancellationToken);
        if (provider is null)
            return Result.Failure<StoredFile>(DomainErrors.Attachments.StorageProviderNotFound);

        if (!provider.IsActive)
            return Result.Failure<StoredFile>(DomainErrors.Attachments.StorageProviderInactive);

        var stored = await _storage.SaveAsync(
            buffer,
            originalFileName,
            mimeType,
            documentId,
            provider.Id,
            provider.BasePathOrEndpoint,
            cancellationToken);

        return Result.Success(stored);
    }
}
