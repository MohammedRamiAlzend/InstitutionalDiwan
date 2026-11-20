using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Storage;
using FluentValidation;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Restores an old version as a brand-new committed version (§3/11); history is never edited.</summary>
public sealed record RestoreDriveFileVersionCommand(Guid ItemId, Guid VersionId) : ICommand<DriveVersionResponse>;

internal sealed class RestoreDriveFileVersionCommandHandler : ICommandHandler<RestoreDriveFileVersionCommand, DriveVersionResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveSpaceRepository _spaces;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveStorageService _storage;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreDriveFileVersionCommandHandler(
        IDriveItemRepository items,
        IDriveSpaceRepository spaces,
        IDriveFileVersionRepository versions,
        IInstitutionSettingRepository settings,
        IDriveStorageService storage,
        IDriveAccessEvaluator access,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _spaces = spaces;
        _versions = versions;
        _settings = settings;
        _storage = storage;
        _access = access;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveVersionResponse>> Handle(RestoreDriveFileVersionCommand request, CancellationToken cancellationToken)
    {
        var resolved = await DriveVersionResolver.ResolveAsync(
            _items, _versions, _access, request.ItemId, request.VersionId, DriveRole.EditorId, cancellationToken);
        if (resolved.IsFailure)
            return Result.Failure<DriveVersionResponse>(resolved.Error);
        var (file, source) = resolved.Value;

        var setting = await _settings.GetSingletonAsync(cancellationToken);
        var space = await _spaces.GetByIdAsync(file.SpaceId, cancellationToken);
        if (space is null)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.SpaceNotFound);
        var effectiveQuota = CreateDriveSpaceCommandHandler.EffectiveQuota(space, setting);
        if (space.UsedBytes + source.SizeBytes > effectiveQuota)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.QuotaExceeded);

        var versionNumber = await _versions.GetNextVersionNumberAsync(file.ItemId, cancellationToken);
        var objectKey = $"{file.SpaceId}/{file.ItemId}/{versionNumber}";
        try
        {
            await _storage.CopyAsync(source.ObjectKey, objectKey, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.StorageNotConfigured);
        }

        var pending = DriveFileVersion.CreatePending(
            file.ItemId, versionNumber, FileStorageProvider.DriveSeaweedFsId, objectKey,
            source.SizeBytes, source.MimeType, DriveVersionSource.RestoreId, _userContext.UserId, _unitOfWork.UtcNow);
        if (pending.IsFailure)
            return Result.Failure<DriveVersionResponse>(pending.Error);

        var version = pending.Value;
        var committed = version.Commit(source.ChecksumSha256, source.SizeBytes, _unitOfWork.UtcNow);
        if (committed.IsFailure)
            return Result.Failure<DriveVersionResponse>(committed.Error);

        var usage = space.AddUsage(source.SizeBytes, effectiveQuota, _unitOfWork.UtcNow);
        if (usage.IsFailure)
            return Result.Failure<DriveVersionResponse>(usage.Error);

        var current = file.SetCurrentVersion(version.VersionId, versionNumber, source.SizeBytes, source.ChecksumSha256, _unitOfWork.UtcNow);
        if (current.IsFailure)
            return Result.Failure<DriveVersionResponse>(current.Error);

        _versions.Add(version);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DriveVersionResponse.From(version, file.CurrentVersionId));
    }
}

public sealed class RestoreDriveFileVersionCommandValidator : AbstractValidator<RestoreDriveFileVersionCommand>
{
    public RestoreDriveFileVersionCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
        RuleFor(command => command.VersionId).NotEmpty().WithErrorCode("Drive.VersionNotFound");
    }
}