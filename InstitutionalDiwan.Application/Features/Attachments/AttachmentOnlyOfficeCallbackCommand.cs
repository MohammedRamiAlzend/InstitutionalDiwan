using InstitutionalDiwan.Application.Abstractions.Drive;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InstitutionalDiwan.Application.Features.Attachments;

public sealed record ProcessAttachmentOnlyOfficeCallbackCommand(
    AttachmentOnlyOfficeCallbackPayload Payload)
    : ICommand<AttachmentOnlyOfficeCallbackResult>;

internal sealed class ProcessAttachmentOnlyOfficeCallbackCommandHandler
    : ICommandHandler<ProcessAttachmentOnlyOfficeCallbackCommand, AttachmentOnlyOfficeCallbackResult>
{
    private const int StatusReadyToSave = 2;
    private const int StatusForceSave = 6;

    private readonly IDocumentFileRepository _files;
    private readonly IDocumentFileTypeRepository _fileTypes;
    private readonly IFileStorageProviderRepository _providers;
    private readonly IDocumentRepository _documents;
    private readonly IDocumentAccessEvaluator _access;
    private readonly IFileStorageService _storage;
    private readonly IDriveCallbackDownloader _downloader;
    private readonly IDriveOnlyOfficeTokenService _tokens;
    private readonly AttachmentUploader _uploader;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessAttachmentOnlyOfficeCallbackCommandHandler> _logger;

    public ProcessAttachmentOnlyOfficeCallbackCommandHandler(
        IDocumentFileRepository files,
        IDocumentFileTypeRepository fileTypes,
        IFileStorageProviderRepository providers,
        IDocumentRepository documents,
        IDocumentAccessEvaluator access,
        IFileStorageService storage,
        IDriveCallbackDownloader downloader,
        IDriveOnlyOfficeTokenService tokens,
        AttachmentUploader uploader,
        IUnitOfWork unitOfWork,
        ILogger<ProcessAttachmentOnlyOfficeCallbackCommandHandler> logger)
    {
        _files = files;
        _fileTypes = fileTypes;
        _providers = providers;
        _documents = documents;
        _access = access;
        _storage = storage;
        _downloader = downloader;
        _tokens = tokens;
        _uploader = uploader;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<AttachmentOnlyOfficeCallbackResult>> Handle(
        ProcessAttachmentOnlyOfficeCallbackCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Payload.Status is not (StatusReadyToSave or StatusForceSave))
            return Result.Success(AttachmentOnlyOfficeCallbackResult.Ok);

        var claims = _tokens.Verify(request.Payload.Token);
        if (claims is null ||
            !Guid.TryParse(GetClaim(claims, "fileId"), out var fileId) ||
            !Guid.TryParse(GetClaim(claims, "versionId"), out var versionId) ||
            !Guid.TryParse(GetClaim(claims, "userId"), out var userId))
            return Result.Success(AttachmentOnlyOfficeCallbackResult.Failed);

        var file = await _files.GetByIdAsync(fileId, cancellationToken);
        var document = file is null ? null : await _documents.GetByIdAsync(file.DocumentId, cancellationToken);
        var baseVersion = file?.Versions.FirstOrDefault(version => version.VersionId == versionId);
        if (file is null || document is null || baseVersion is null || !baseVersion.IsCurrent || file.IsArchived)
            return Result.Success(AttachmentOnlyOfficeCallbackResult.Failed);

        if (!await _access.CanAccessDocumentAsync(file.DocumentId, AccessLevel.ManageId, cancellationToken))
            return Result.Success(AttachmentOnlyOfficeCallbackResult.Failed);

        if (document.EnsureNotArchived().IsFailure ||
            file.Versions.All(version => !version.IsCurrent || version.VersionId != versionId))
            return Result.Success(AttachmentOnlyOfficeCallbackResult.Failed);

        var fileType = await _fileTypes.GetByIdAsync(file.FileTypeId, cancellationToken);
        if (fileType is null || !fileType.IsActive || string.IsNullOrWhiteSpace(request.Payload.Url))
            return Result.Success(AttachmentOnlyOfficeCallbackResult.Failed);

        var downloaded = await _downloader.DownloadAsync(request.Payload.Url, cancellationToken);
        if (downloaded is null)
            return Result.Success(AttachmentOnlyOfficeCallbackResult.Failed);

        await using (downloaded)
        {
            await using var buffer = new MemoryStream();
            await downloaded.CopyToAsync(buffer, cancellationToken);
            if (buffer.Length <= 0)
                return Result.Success(AttachmentOnlyOfficeCallbackResult.Failed);

            var bytes = buffer.ToArray();
            await using var checksumStream = new MemoryStream(bytes, writable: false);
            var checksum = await _storage.ComputeSha256Async(checksumStream, cancellationToken);
            if (string.Equals(checksum, baseVersion.ChecksumSha256, StringComparison.OrdinalIgnoreCase))
                return Result.Success(AttachmentOnlyOfficeCallbackResult.Ok);

            await using var content = new MemoryStream(bytes, writable: false);
            var stored = await _uploader.StoreAsync(
                file.DocumentId,
                fileType,
                content,
                baseVersion.OriginalFileName,
                baseVersion.MimeType,
                bytes.LongLength,
                cancellationToken);
            if (stored.IsFailure)
            {
                _logger.LogWarning("OnlyOffice attachment save rejected for {FileId}: {Code}.", fileId, stored.Error.Code);
                return Result.Success(AttachmentOnlyOfficeCallbackResult.Failed);
            }

            var version = file.AddVersion(
                stored.Value.StorageProviderId,
                stored.Value.RelativePath,
                stored.Value.OriginalFileName,
                stored.Value.MimeType,
                stored.Value.FileSizeBytes,
                stored.Value.ChecksumSha256,
                userId,
                "تم الحفظ عبر OnlyOffice",
                _unitOfWork.UtcNow);
            if (version.IsFailure)
                return Result.Success(AttachmentOnlyOfficeCallbackResult.Failed);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return Result.Success(AttachmentOnlyOfficeCallbackResult.Ok);
    }

    private static string? GetClaim(IReadOnlyDictionary<string, object?> claims, string key) =>
        claims.TryGetValue(key, out var value) ? value?.ToString() : null;
}
