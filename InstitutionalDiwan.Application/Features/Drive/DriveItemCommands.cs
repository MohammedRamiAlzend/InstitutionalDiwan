using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Creates a folder. At a space root only the owner/ADMIN may create; inside a folder, Editor+.</summary>
public sealed record CreateDriveFolderCommand(Guid SpaceId, Guid? ParentId, string Name) : ICommand<DriveItemResponse>;

/// <summary>Renames a folder or file (Editor+).</summary>
public sealed record RenameDriveItemCommand(Guid ItemId, string Name) : ICommand<DriveItemResponse>;

/// <summary>Moves a node under a new parent (null = current space root). Cross-space moves need Manage.</summary>
public sealed record MoveDriveItemCommand(Guid ItemId, Guid? NewParentId) : ICommand<DriveItemResponse>;

/// <summary>Stars an item for the caller (personal, idempotent, Viewer+).</summary>
public sealed record StarDriveItemCommand(Guid ItemId) : ICommand;

/// <summary>Removes the caller's star (idempotent).</summary>
public sealed record UnstarDriveItemCommand(Guid ItemId) : ICommand;

internal sealed class CreateDriveFolderCommandHandler : ICommandHandler<CreateDriveFolderCommand, DriveItemResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveSpaceRepository _spaces;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDriveFolderCommandHandler(
        IDriveItemRepository items,
        IDriveSpaceRepository spaces,
        IDriveAccessEvaluator access,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _spaces = spaces;
        _access = access;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveItemResponse>> Handle(CreateDriveFolderCommand request, CancellationToken cancellationToken)
    {
        var space = await _spaces.GetByIdAsync(request.SpaceId, cancellationToken);
        if (space is null || space.IsTrashed)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.SpaceNotFound);

        var depth = 1;
        if (request.ParentId is { } parentId)
        {
            var parent = await _items.GetByIdAsync(parentId, cancellationToken);
            if (parent is null || parent.SpaceId != request.SpaceId)
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemNotFound);
            if (!parent.IsFolder)
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.NotAFolder);
            if (!await _access.CanAccessItemAsync(parentId, DriveRole.EditorId, cancellationToken))
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);
            depth = await _items.GetDepthAsync(parentId, cancellationToken) + 1;
        }
        else if (space.OwnerUserId != _userContext.UserId && !_access.IsAdmin)
        {
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);
        }

        if (await _items.NameExistsAsync(request.SpaceId, request.ParentId, request.Name.Trim(), null, cancellationToken))
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.NameTaken);

        var created = DriveItem.CreateFolder(
            request.SpaceId, request.ParentId, request.Name, _userContext.UserId, depth, _unitOfWork.UtcNow);
        if (created.IsFailure)
            return Result.Failure<DriveItemResponse>(created.Error);

        _items.Add(created.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DriveItemResponse.From(created.Value));
    }
}

internal sealed class RenameDriveItemCommandHandler : ICommandHandler<RenameDriveItemCommand, DriveItemResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUnitOfWork _unitOfWork;

    public RenameDriveItemCommandHandler(
        IDriveItemRepository items,
        IDriveAccessEvaluator access,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _access = access;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveItemResponse>> Handle(RenameDriveItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemNotFound);
        if (!await _access.CanAccessItemAsync(request.ItemId, DriveRole.EditorId, cancellationToken))
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);

        if (await _items.NameExistsAsync(item.SpaceId, item.ParentId, request.Name.Trim(), item.ItemId, cancellationToken))
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.NameTaken);

        var renamed = item.Rename(request.Name, _unitOfWork.UtcNow);
        if (renamed.IsFailure)
            return Result.Failure<DriveItemResponse>(renamed.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DriveItemResponse.From(item));
    }
}

internal sealed class MoveDriveItemCommandHandler : ICommandHandler<MoveDriveItemCommand, DriveItemResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveSpaceRepository _spaces;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public MoveDriveItemCommandHandler(
        IDriveItemRepository items,
        IDriveSpaceRepository spaces,
        IDriveAccessEvaluator access,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _spaces = spaces;
        _access = access;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveItemResponse>> Handle(MoveDriveItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemNotFound);

        var requiredRole = DriveRole.EditorId;
        Guid newSpaceId = item.SpaceId;
        Guid? newParentId = request.NewParentId;
        IReadOnlyCollection<Guid> targetAncestorIds = Array.Empty<Guid>();
        var targetDepth = 1;

        if (request.NewParentId is { } parentId)
        {
            var parent = await _items.GetByIdAsync(parentId, cancellationToken);
            if (parent is null)
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemNotFound);
            if (!parent.IsFolder)
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.NotAFolder);
            if (parent.IsTrashed)
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemTrashed);

            newSpaceId = parent.SpaceId;
            if (newSpaceId != item.SpaceId)
                requiredRole = DriveRole.ManagerId;
            if (!await _access.CanAccessItemAsync(parentId, DriveRole.EditorId, cancellationToken))
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);

            targetAncestorIds = await _items.GetAncestorIdsAsync(parentId, cancellationToken);
            targetDepth = await _items.GetDepthAsync(parentId, cancellationToken) + 1;
        }
        else
        {
            var space = await _spaces.GetByIdAsync(newSpaceId, cancellationToken);
            if (space is null || space.IsTrashed)
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.SpaceNotFound);
            if (space.OwnerUserId != _userContext.UserId && !_access.IsAdmin)
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);
        }

        if (!await _access.CanAccessItemAsync(request.ItemId, requiredRole, cancellationToken))
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);

        if (await _items.NameExistsAsync(newSpaceId, newParentId, item.Name, item.ItemId, cancellationToken))
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.NameTaken);

        var moved = item.Move(newSpaceId, newParentId, targetAncestorIds, targetDepth, _unitOfWork.UtcNow);
        if (moved.IsFailure)
            return Result.Failure<DriveItemResponse>(moved.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DriveItemResponse.From(item));
    }
}

internal sealed class StarDriveItemCommandHandler : ICommandHandler<StarDriveItemCommand>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveStarRepository _stars;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public StarDriveItemCommandHandler(
        IDriveItemRepository items,
        IDriveStarRepository stars,
        IDriveAccessEvaluator access,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _stars = stars;
        _access = access;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(StarDriveItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure(DomainErrors.Drive.ItemNotFound);
        if (!await _access.CanAccessItemAsync(request.ItemId, DriveRole.ViewerId, cancellationToken))
            return Result.Failure(DomainErrors.Drive.AccessDenied);

        var existing = await _stars.FindAsync(_userContext.UserId, request.ItemId, cancellationToken);
        if (existing is not null)
            return Result.Success();

        var star = DriveStar.Create(_userContext.UserId, request.ItemId, _unitOfWork.UtcNow);
        if (star.IsFailure)
            return star;

        _stars.Add(star.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class UnstarDriveItemCommandHandler : ICommandHandler<UnstarDriveItemCommand>
{
    private readonly IDriveStarRepository _stars;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public UnstarDriveItemCommandHandler(
        IDriveStarRepository stars,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _stars = stars;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(UnstarDriveItemCommand request, CancellationToken cancellationToken)
    {
        var existing = await _stars.FindAsync(_userContext.UserId, request.ItemId, cancellationToken);
        if (existing is null)
            return Result.Success();

        _stars.Remove(existing);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}