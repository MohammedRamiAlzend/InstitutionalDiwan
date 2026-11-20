using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Streams a file's bytes (current or a specific committed version), optionally an HTTP Range.</summary>
public sealed record GetDriveFileContentQuery(
    Guid ItemId,
    Guid? VersionId = null,
    long? RangeStart = null,
    long? RangeEnd = null) : IQuery<DriveFileContentResult>;

/// <summary>Signs a short-lived presigned download URL for the browser or OnlyOffice.</summary>
public sealed record GetDriveDownloadUrlQuery(Guid ItemId, Guid? VersionId = null) : IQuery<DriveDownloadUrlResponse>;

/// <summary>Lists a file's versions, newest first.</summary>
public sealed record ListDriveFileVersionsQuery(Guid ItemId) : IQuery<IReadOnlyList<DriveVersionResponse>>;

internal sealed class GetDriveFileContentQueryHandler : IQueryHandler<GetDriveFileContentQuery, DriveFileContentResult>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IDriveStorageService _storage;
    private readonly IDriveAccessEvaluator _access;

    public GetDriveFileContentQueryHandler(
        IDriveItemRepository items,
        IDriveFileVersionRepository versions,
        IDriveStorageService storage,
        IDriveAccessEvaluator access)
    {
        _items = items;
        _versions = versions;
        _storage = storage;
        _access = access;
    }

    public async Task<Result<DriveFileContentResult>> Handle(GetDriveFileContentQuery request, CancellationToken cancellationToken)
    {
        var resolved = await DriveVersionResolver.ResolveAsync(_items, _versions, _access, request.ItemId, request.VersionId, DriveRole.ViewerId, cancellationToken);
        if (resolved.IsFailure)
            return Result.Failure<DriveFileContentResult>(resolved.Error);
        var (file, version) = resolved.Value;

        var stream = await _storage.OpenReadAsync(version.ObjectKey, request.RangeStart, request.RangeEnd, cancellationToken);
        if (stream is null)
            return Result.Failure<DriveFileContentResult>(DomainErrors.Drive.StorageObjectMissing);

        long? rangeStart = request.RangeStart;
        long? rangeEnd = request.RangeEnd;
        if (rangeStart is { } start && stream.CanSeek)
            rangeEnd = Math.Min(rangeEnd ?? version.SizeBytes - 1, version.SizeBytes - 1);

        return Result.Success(new DriveFileContentResult(
            stream, version.MimeType, file.Name, version.SizeBytes, rangeStart, rangeEnd));
    }
}

internal sealed class GetDriveDownloadUrlQueryHandler : IQueryHandler<GetDriveDownloadUrlQuery, DriveDownloadUrlResponse>
{
    private static readonly TimeSpan DownloadLifetime = TimeSpan.FromMinutes(5);

    private readonly IDriveItemRepository _items;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IDriveStorageService _storage;
    private readonly IDriveAccessEvaluator _access;

    public GetDriveDownloadUrlQueryHandler(
        IDriveItemRepository items,
        IDriveFileVersionRepository versions,
        IDriveStorageService storage,
        IDriveAccessEvaluator access)
    {
        _items = items;
        _versions = versions;
        _storage = storage;
        _access = access;
    }

    public async Task<Result<DriveDownloadUrlResponse>> Handle(GetDriveDownloadUrlQuery request, CancellationToken cancellationToken)
    {
        var resolved = await DriveVersionResolver.ResolveAsync(_items, _versions, _access, request.ItemId, request.VersionId, DriveRole.ViewerId, cancellationToken);
        if (resolved.IsFailure)
            return Result.Failure<DriveDownloadUrlResponse>(resolved.Error);
        var (_, version) = resolved.Value;

        try
        {
            var url = await _storage.CreateDownloadUrlAsync(version.ObjectKey, DownloadLifetime, cancellationToken);
            return Result.Success(new DriveDownloadUrlResponse(url, DateTime.UtcNow.Add(DownloadLifetime)));
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<DriveDownloadUrlResponse>(DomainErrors.Drive.StorageNotConfigured);
        }
    }
}

internal sealed class ListDriveFileVersionsQueryHandler : IQueryHandler<ListDriveFileVersionsQuery, IReadOnlyList<DriveVersionResponse>>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IDriveAccessEvaluator _access;

    public ListDriveFileVersionsQueryHandler(
        IDriveItemRepository items,
        IDriveFileVersionRepository versions,
        IDriveAccessEvaluator access)
    {
        _items = items;
        _versions = versions;
        _access = access;
    }

    public async Task<Result<IReadOnlyList<DriveVersionResponse>>> Handle(ListDriveFileVersionsQuery request, CancellationToken cancellationToken)
    {
        var file = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (file is null)
            return Result.Failure<IReadOnlyList<DriveVersionResponse>>(DomainErrors.Drive.ItemNotFound);
        if (!file.IsFile)
            return Result.Failure<IReadOnlyList<DriveVersionResponse>>(DomainErrors.Drive.NotAFile);
        if (!await _access.CanAccessItemAsync(request.ItemId, DriveRole.ViewerId, cancellationToken))
            return Result.Failure<IReadOnlyList<DriveVersionResponse>>(DomainErrors.Drive.AccessDenied);

        var versions = await _versions.ListByItemAsync(request.ItemId, cancellationToken);
        return Result.Success((IReadOnlyList<DriveVersionResponse>)versions
            .OrderByDescending(version => version.VersionNumber)
            .Select(version => DriveVersionResponse.From(version, file.CurrentVersionId))
            .ToList());
    }
}

/// <summary>Resolves the file item and the requested (or current) committed version, gating access.</summary>
internal static class DriveVersionResolver
{
    public static async Task<Result<(DriveItem File, DriveFileVersion Version)>> ResolveAsync(
        IDriveItemRepository items,
        IDriveFileVersionRepository versions,
        IDriveAccessEvaluator access,
        Guid itemId,
        Guid? versionId,
        int requiredRoleId,
        CancellationToken cancellationToken)
    {
        var file = await items.GetByIdAsync(itemId, cancellationToken);
        if (file is null)
            return Result.Failure<(DriveItem, DriveFileVersion)>(DomainErrors.Drive.ItemNotFound);
        if (!file.IsFile)
            return Result.Failure<(DriveItem, DriveFileVersion)>(DomainErrors.Drive.NotAFile);
        if (!await access.CanAccessItemAsync(itemId, requiredRoleId, cancellationToken))
            return Result.Failure<(DriveItem, DriveFileVersion)>(DomainErrors.Drive.AccessDenied);

        DriveFileVersion? version;
        if (versionId is { } requested)
        {
            version = await versions.GetByIdAsync(requested, cancellationToken);
            if (version is null || version.FileItemId != itemId)
                return Result.Failure<(DriveItem, DriveFileVersion)>(DomainErrors.Drive.VersionNotFound);
        }
        else
        {
            if (file.CurrentVersionId is not { } currentId)
                return Result.Failure<(DriveItem, DriveFileVersion)>(DomainErrors.Drive.VersionNotFound);
            version = await versions.GetByIdAsync(currentId, cancellationToken);
        }

        if (version is null || !version.IsCommitted)
            return Result.Failure<(DriveItem, DriveFileVersion)>(DomainErrors.Drive.VersionNotFound);

        return Result.Success((file, version));
    }
}