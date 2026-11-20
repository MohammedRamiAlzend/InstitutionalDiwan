using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>
/// Reserves a pending version and signs a direct PUT upload (§7.1). With <paramref name="FileItemId"/>
/// it adds a version to an existing file; without it, it creates a new file at the destination.
/// </summary>
public sealed record InitiateDriveUploadCommand(
    Guid SpaceId,
    Guid? ParentId,
    string FileName,
    long DeclaredSizeBytes,
    string? MimeType = null,
    Guid? FileItemId = null) : ICommand<DriveUploadInitiatedResponse>;

/// <summary>Verifies the stored object (size, checksum, magic bytes) and commits the pending version.</summary>
public sealed record CompleteDriveUploadCommand(Guid VersionId, string? ChecksumSha256 = null) : ICommand<DriveVersionResponse>;

/// <summary>Abandons a pending upload and removes any partial object.</summary>
public sealed record AbandonDriveUploadCommand(Guid VersionId) : ICommand;

internal sealed class InitiateDriveUploadCommandHandler : ICommandHandler<InitiateDriveUploadCommand, DriveUploadInitiatedResponse>
{
    private static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(15);

    private readonly IDriveItemRepository _items;
    private readonly IDriveSpaceRepository _spaces;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveStorageService _storage;
    private readonly IDriveAccessEvaluator _access;
    private readonly DriveUploadGuard _guard;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public InitiateDriveUploadCommandHandler(
        IDriveItemRepository items,
        IDriveSpaceRepository spaces,
        IDriveFileVersionRepository versions,
        IInstitutionSettingRepository settings,
        IDriveStorageService storage,
        IDriveAccessEvaluator access,
        DriveUploadGuard guard,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _spaces = spaces;
        _versions = versions;
        _settings = settings;
        _storage = storage;
        _access = access;
        _guard = guard;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveUploadInitiatedResponse>> Handle(InitiateDriveUploadCommand request, CancellationToken cancellationToken)
    {
        var setting = await _settings.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.StorageNotConfigured);

        var declared = _guard.CheckDeclared(request.FileName, request.DeclaredSizeBytes, setting);
        if (declared.IsFailure)
            return Result.Failure<DriveUploadInitiatedResponse>(declared.Error);

        DriveItem file;
        int versionNumber;
        if (request.FileItemId is { } fileItemId)
        {
            var existing = await _items.GetByIdAsync(fileItemId, cancellationToken);
            if (existing is null)
                return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.ItemNotFound);
            if (!existing.IsFile)
                return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.NotAFile);
            if (!await _access.CanAccessItemAsync(fileItemId, DriveRole.EditorId, cancellationToken))
                return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.AccessDenied);

            file = existing;
            versionNumber = await _versions.GetNextVersionNumberAsync(fileItemId, cancellationToken);
        }
        else
        {
            var space = await _spaces.GetByIdAsync(request.SpaceId, cancellationToken);
            if (space is null || space.IsTrashed)
                return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.SpaceNotFound);

            var depth = 1;
            if (request.ParentId is { } parentId)
            {
                var parent = await _items.GetByIdAsync(parentId, cancellationToken);
                if (parent is null || parent.SpaceId != request.SpaceId)
                    return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.ItemNotFound);
                if (!parent.IsFolder)
                    return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.NotAFolder);
                if (!await _access.CanAccessItemAsync(parentId, DriveRole.EditorId, cancellationToken))
                    return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.AccessDenied);
                depth = await _items.GetDepthAsync(parentId, cancellationToken) + 1;
            }
            else if (space.OwnerUserId != _userContext.UserId && !_access.IsAdmin)
            {
                return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.AccessDenied);
            }

            if (await _items.NameExistsAsync(request.SpaceId, request.ParentId, request.FileName.Trim(), null, cancellationToken))
                return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.NameTaken);

            var created = DriveItem.CreateFile(
                request.SpaceId, request.ParentId, request.FileName, _userContext.UserId, depth, _unitOfWork.UtcNow);
            if (created.IsFailure)
                return Result.Failure<DriveUploadInitiatedResponse>(created.Error);

            file = created.Value;
            _items.Add(file);
            versionNumber = 1;
        }

        var ownerSpace = await _spaces.GetByIdAsync(file.SpaceId, cancellationToken);
        if (ownerSpace is null)
            return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.SpaceNotFound);
        var effectiveQuota = CreateDriveSpaceCommandHandler.EffectiveQuota(ownerSpace, setting);
        if (ownerSpace.UsedBytes + request.DeclaredSizeBytes > effectiveQuota)
            return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.QuotaExceeded);

        var objectKey = $"{file.SpaceId}/{file.ItemId}/{versionNumber}";
        var pending = DriveFileVersion.CreatePending(
            file.ItemId, versionNumber, FileStorageProvider.DriveSeaweedFsId, objectKey,
            request.DeclaredSizeBytes, request.MimeType, DriveVersionSource.UploadId, _userContext.UserId, _unitOfWork.UtcNow);
        if (pending.IsFailure)
            return Result.Failure<DriveUploadInitiatedResponse>(pending.Error);

        DrivePresignedUpload upload;
        try
        {
            upload = await _storage.CreateUploadUrlAsync(
                objectKey, request.MimeType, request.DeclaredSizeBytes, UploadLifetime, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<DriveUploadInitiatedResponse>(DomainErrors.Drive.StorageNotConfigured);
        }

        _versions.Add(pending.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new DriveUploadInitiatedResponse(
            pending.Value.VersionId, file.ItemId, versionNumber, objectKey, upload.Url, upload.ExpiresAtUtc));
    }
}

internal sealed class CompleteDriveUploadCommandHandler : ICommandHandler<CompleteDriveUploadCommand, DriveVersionResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveSpaceRepository _spaces;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveStorageService _storage;
    private readonly IDriveAccessEvaluator _access;
    private readonly DriveUploadGuard _guard;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteDriveUploadCommandHandler(
        IDriveItemRepository items,
        IDriveSpaceRepository spaces,
        IDriveFileVersionRepository versions,
        IInstitutionSettingRepository settings,
        IDriveStorageService storage,
        IDriveAccessEvaluator access,
        DriveUploadGuard guard,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _spaces = spaces;
        _versions = versions;
        _settings = settings;
        _storage = storage;
        _access = access;
        _guard = guard;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveVersionResponse>> Handle(CompleteDriveUploadCommand request, CancellationToken cancellationToken)
    {
        var version = await _versions.GetByIdAsync(request.VersionId, cancellationToken);
        if (version is null)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.VersionNotFound);
        if (!version.IsPending)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.VersionNotPending);

        var file = await _items.GetByIdAsync(version.FileItemId, cancellationToken);
        if (file is null)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.ItemNotFound);
        if (!await _access.CanAccessItemAsync(file.ItemId, DriveRole.EditorId, cancellationToken))
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.AccessDenied);

        var setting = await _settings.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.StorageNotConfigured);

        var head = await _storage.HeadAsync(version.ObjectKey, cancellationToken);
        if (head is null)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.StorageObjectMissing);
        if (head.SizeBytes != version.SizeBytes)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.DeclaredSizeMismatch);
        if (head.SizeBytes > setting.DriveMaxFileSizeBytes)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.FileTooLarge);

        var header = await ReadHeaderAsync(version.ObjectKey, head.SizeBytes, cancellationToken);
        var signature = _guard.CheckSignature(header, file.Name, head.SizeBytes, setting);
        if (signature.IsFailure)
            return Result.Failure<DriveVersionResponse>(signature.Error);

        string checksum;
        await using (var content = await _storage.OpenReadAsync(version.ObjectKey, null, null, cancellationToken))
        {
            if (content is null)
                return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.StorageObjectMissing);
            checksum = await _storage.ComputeSha256Async(content, cancellationToken);
        }

        if (request.ChecksumSha256 is { } declaredChecksum &&
            !string.Equals(declaredChecksum.Trim(), checksum, StringComparison.OrdinalIgnoreCase))
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.SignatureMismatch);

        var space = await _spaces.GetByIdAsync(file.SpaceId, cancellationToken);
        if (space is null)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.SpaceNotFound);
        var effectiveQuota = CreateDriveSpaceCommandHandler.EffectiveQuota(space, setting);

        var usage = space.AddUsage(head.SizeBytes, effectiveQuota, _unitOfWork.UtcNow);
        if (usage.IsFailure)
            return Result.Failure<DriveVersionResponse>(usage.Error);

        var committed = version.Commit(checksum, head.SizeBytes, _unitOfWork.UtcNow);
        if (committed.IsFailure)
            return Result.Failure<DriveVersionResponse>(committed.Error);

        var current = file.SetCurrentVersion(version.VersionId, version.VersionNumber, head.SizeBytes, checksum, _unitOfWork.UtcNow);
        if (current.IsFailure)
            return Result.Failure<DriveVersionResponse>(current.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DriveVersionResponse.From(version, file.CurrentVersionId));
    }

    private async Task<byte[]> ReadHeaderAsync(string objectKey, long sizeBytes, CancellationToken cancellationToken)
    {
        const int signatureLength = 16;
        var take = (int)Math.Min(signatureLength, sizeBytes);
        await using var stream = await _storage.OpenReadAsync(objectKey, 0, take - 1, cancellationToken);
        if (stream is null)
            return Array.Empty<byte>();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken);
        return buffer.ToArray();
    }
}

internal sealed class AbandonDriveUploadCommandHandler : ICommandHandler<AbandonDriveUploadCommand>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IDriveStorageService _storage;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUnitOfWork _unitOfWork;

    public AbandonDriveUploadCommandHandler(
        IDriveItemRepository items,
        IDriveFileVersionRepository versions,
        IDriveStorageService storage,
        IDriveAccessEvaluator access,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _versions = versions;
        _storage = storage;
        _access = access;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(AbandonDriveUploadCommand request, CancellationToken cancellationToken)
    {
        var version = await _versions.GetByIdAsync(request.VersionId, cancellationToken);
        if (version is null)
            return Result.Failure(DomainErrors.Drive.VersionNotFound);
        if (!version.IsPending)
            return Result.Failure(DomainErrors.Drive.VersionNotPending);

        var file = await _items.GetByIdAsync(version.FileItemId, cancellationToken);
        if (file is null)
            return Result.Failure(DomainErrors.Drive.ItemNotFound);
        if (!await _access.CanAccessItemAsync(file.ItemId, DriveRole.EditorId, cancellationToken))
            return Result.Failure(DomainErrors.Drive.AccessDenied);

        var abandoned = version.Abandon();
        if (abandoned.IsFailure)
            return abandoned;

        await _storage.DeleteAsync(version.ObjectKey, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}