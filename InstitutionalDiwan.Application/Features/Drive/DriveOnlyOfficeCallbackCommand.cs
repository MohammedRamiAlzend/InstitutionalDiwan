using InstitutionalDiwan.Application.Abstractions.Drive;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>
/// Processes the OnlyOffice save callback (Module 11, §7.2). There is no ambient user session — the
/// caller is OnlyOffice, authenticated by the HS256 callback token — so the acting user is the edit
/// session's owner. The <see cref="Result{T}"/> always succeeds (the callback is acknowledged); the
/// OnlyOffice protocol error code lives in <see cref="DriveOnlyOfficeCallbackResult"/> so the
/// transaction commits and a save-failure notification to the editor is persisted even on rejection.
/// </summary>
public sealed record ProcessDriveOnlyOfficeCallbackCommand(DriveOnlyOfficeCallbackPayload Payload)
    : ICommand<DriveOnlyOfficeCallbackResult>;

internal sealed class ProcessDriveOnlyOfficeCallbackCommandHandler
    : ICommandHandler<ProcessDriveOnlyOfficeCallbackCommand, DriveOnlyOfficeCallbackResult>
{
    private const int StatusEditing = 1;
    private const int StatusReadyToSave = 2;
    private const int StatusClosedNoChange = 4;
    private const int StatusForceSave = 6;

    private readonly IDriveItemRepository _items;
    private readonly IDriveSpaceRepository _spaces;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IDriveEditSessionRepository _sessions;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveStorageService _storage;
    private readonly IDriveCallbackDownloader _downloader;
    private readonly IDriveOnlyOfficeTokenService _tokens;
    private readonly DriveUploadGuard _guard;
    private readonly IDriveNotificationPublisher _notifications;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessDriveOnlyOfficeCallbackCommandHandler> _logger;

    public ProcessDriveOnlyOfficeCallbackCommandHandler(
        IDriveItemRepository items,
        IDriveSpaceRepository spaces,
        IDriveFileVersionRepository versions,
        IDriveEditSessionRepository sessions,
        IInstitutionSettingRepository settings,
        IDriveStorageService storage,
        IDriveCallbackDownloader downloader,
        IDriveOnlyOfficeTokenService tokens,
        DriveUploadGuard guard,
        IDriveNotificationPublisher notifications,
        IUnitOfWork unitOfWork,
        ILogger<ProcessDriveOnlyOfficeCallbackCommandHandler> logger)
    {
        _items = items;
        _spaces = spaces;
        _versions = versions;
        _sessions = sessions;
        _settings = settings;
        _storage = storage;
        _downloader = downloader;
        _tokens = tokens;
        _guard = guard;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<DriveOnlyOfficeCallbackResult>> Handle(
        ProcessDriveOnlyOfficeCallbackCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Payload;

        var verified = _tokens.Verify(payload.Token);
        if (verified is null)
        {
            _logger.LogWarning("OnlyOffice callback rejected: missing or invalid token.");
            return Ack(DriveOnlyOfficeCallbackResult.Failed);
        }

        var key = payload.Key;
        if (string.IsNullOrWhiteSpace(key) && verified.TryGetValue("key", out var keyClaim))
            key = keyClaim?.ToString();
        if (string.IsNullOrWhiteSpace(key))
            return Ack(DriveOnlyOfficeCallbackResult.Failed);

        var session = await _sessions.FindOpenByKeyAsync(key, cancellationToken);

        switch (payload.Status)
        {
            case StatusEditing:
                if (session is not null && session.IsOpen)
                {
                    session.Touch(_unitOfWork.UtcNow);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                return Ack(DriveOnlyOfficeCallbackResult.Ok);

            case StatusClosedNoChange:
                if (session is not null)
                {
                    session.Close(_unitOfWork.UtcNow);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                return Ack(DriveOnlyOfficeCallbackResult.Ok);

            case StatusReadyToSave:
            case StatusForceSave:
                if (session is null)
                {
                    _logger.LogWarning("OnlyOffice save callback for key {Key} has no open session.", key);
                    return Ack(DriveOnlyOfficeCallbackResult.Failed);
                }
                return Ack(await SaveAsync(session, payload, cancellationToken));

            default:
                return Ack(DriveOnlyOfficeCallbackResult.Ok);
        }
    }

    private async Task<DriveOnlyOfficeCallbackResult> SaveAsync(
        DriveEditSession session, DriveOnlyOfficeCallbackPayload payload, CancellationToken cancellationToken)
    {
        var now = _unitOfWork.UtcNow;
        var file = await _items.GetByIdAsync(session.FileItemId, cancellationToken);
        if (file is null || !file.IsFile)
            return await FailAsync(session, file?.Name ?? "ملف", DomainErrors.Drive.ItemNotFound, cancellationToken);
        if (file.IsTrashed)
            return await FailAsync(session, file.Name, DomainErrors.Drive.ItemTrashed, cancellationToken);

        // Lost-update guard: the file moved on since this session was opened (also applied to
        // forcesave, so an intermediate save can never silently overwrite a newer version).
        if (file.CurrentVersionId != session.BaseVersionId)
            return await FailAsync(session, file.Name, DomainErrors.Drive.VersionConflict, cancellationToken);

        var baseVersion = await _versions.GetByIdAsync(session.BaseVersionId, cancellationToken);

        if (string.IsNullOrWhiteSpace(payload.Url))
            return await FailAsync(session, file.Name, DomainErrors.Drive.CallbackDownloadFailed, cancellationToken);

        var downloaded = await _downloader.DownloadAsync(payload.Url, cancellationToken);
        if (downloaded is null)
            return await FailAsync(session, file.Name, DomainErrors.Drive.CallbackDownloadFailed, cancellationToken);

        byte[] bytes;
        await using (downloaded)
        {
            using var buffer = new MemoryStream();
            await downloaded.CopyToAsync(buffer, cancellationToken);
            bytes = buffer.ToArray();
        }

        var size = bytes.LongLength;
        if (size <= 0)
            return await FailAsync(session, file.Name, DomainErrors.Drive.VersionSizeInvalid, cancellationToken);

        string checksum;
        await using (var checksumStream = new MemoryStream(bytes))
            checksum = await _storage.ComputeSha256Async(checksumStream, cancellationToken);

        // Dedup: an unchanged save (OnlyOffice repeats callbacks) updates the session, not the history.
        if (baseVersion is not null &&
            string.Equals(checksum, baseVersion.ChecksumSha256, StringComparison.OrdinalIgnoreCase))
        {
            session.MarkSaved(now);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return DriveOnlyOfficeCallbackResult.Ok;
        }

        var setting = await _settings.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return await FailAsync(session, file.Name, DomainErrors.Drive.StorageNotConfigured, cancellationToken);
        if (size > setting.DriveMaxFileSizeBytes)
            return await FailAsync(session, file.Name, DomainErrors.Drive.FileTooLarge, cancellationToken);

        var signature = _guard.CheckSignature(bytes.AsSpan(0, Math.Min(16, bytes.Length)), file.Name, size, setting);
        if (signature.IsFailure)
            return await FailAsync(session, file.Name, signature.Error, cancellationToken);

        var space = await _spaces.GetByIdAsync(file.SpaceId, cancellationToken);
        if (space is null)
            return await FailAsync(session, file.Name, DomainErrors.Drive.SpaceNotFound, cancellationToken);
        var effectiveQuota = CreateDriveSpaceCommandHandler.EffectiveQuota(space, setting);
        if (space.UsedBytes + size > effectiveQuota)
            return await FailAsync(session, file.Name, DomainErrors.Drive.QuotaExceeded, cancellationToken);

        var versionNumber = await _versions.GetNextVersionNumberAsync(file.ItemId, cancellationToken);
        var objectKey = $"{file.SpaceId}/{file.ItemId}/{versionNumber}";
        var mimeType = baseVersion?.MimeType ?? "application/octet-stream";
        try
        {
            await using var writeStream = new MemoryStream(bytes);
            await _storage.WriteAsync(objectKey, writeStream, mimeType, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return await FailAsync(session, file.Name, DomainErrors.Drive.StorageNotConfigured, cancellationToken);
        }

        // All guards passed; the mutations below cannot fail (file is a live, untrashed file, quota
        // and checksum are verified, the session is open), so they commit together with the audit rows.
        var pending = DriveFileVersion.CreatePending(
            file.ItemId, versionNumber, FileStorageProvider.DriveSeaweedFsId, objectKey,
            size, mimeType, DriveVersionSource.EditorSaveId, session.UserId, now);
        var version = pending.Value;
        version.Commit(checksum, size, now);
        space.AddUsage(size, effectiveQuota, now);
        file.SetCurrentVersion(version.VersionId, versionNumber, size, checksum, now);
        session.MarkSaved(now);
        _versions.Add(version);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DriveOnlyOfficeCallbackResult.Ok;
    }

    private async Task<DriveOnlyOfficeCallbackResult> FailAsync(
        DriveEditSession session, string itemTitle, Error error, CancellationToken cancellationToken)
    {
        _logger.LogWarning("OnlyOffice save for session {SessionId} rejected: {Code}.", session.SessionId, error.Code);
        // Actor is Guid.Empty so the editor (the session owner) is not dropped from the audience.
        await _notifications.PublishAsync(
            NotificationType.DriveEditSaveFailedId, new[] { session.UserId }, itemTitle, Guid.Empty,
            cancellationToken: cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return DriveOnlyOfficeCallbackResult.Failed;
    }

    private static Result<DriveOnlyOfficeCallbackResult> Ack(DriveOnlyOfficeCallbackResult answer) =>
        Result.Success(answer);
}