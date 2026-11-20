using FluentValidation;
using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Drive;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Restores a trashed item; if its parent is still trashed it returns to the space root (§7.3).</summary>
public sealed record RestoreDriveItemCommand(Guid ItemId) : ICommand<DriveItemResponse>;

/// <summary>Permanently deletes a trashed item tree. ADMIN or department head with Manage (§3/4).</summary>
public sealed record PurgeDriveItemCommand(Guid ItemId) : ICommand;

internal sealed class RestoreDriveItemCommandHandler : ICommandHandler<RestoreDriveItemCommand, DriveItemResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreDriveItemCommandHandler(
        IDriveItemRepository items,
        IDriveAccessEvaluator access,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _access = access;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveItemResponse>> Handle(RestoreDriveItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemNotFound);
        if (!item.IsTrashed)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemNotTrashed);
        if (!await _access.CanAccessItemAsync(request.ItemId, DriveRole.ManagerId, cancellationToken))
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);

        var destinationParentId = item.ParentId;
        if (item.ParentId is { } parentId)
        {
            var parent = await _items.GetByIdAsync(parentId, cancellationToken);
            if (parent is null || parent.IsTrashed)
                destinationParentId = null;
        }

        if (await _items.NameExistsAsync(item.SpaceId, destinationParentId, item.Name, item.ItemId, cancellationToken))
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.NameTaken);

        var now = _unitOfWork.UtcNow;
        var restored = item.Restore(now);
        if (restored.IsFailure)
            return Result.Failure<DriveItemResponse>(restored.Error);

        if (destinationParentId != item.ParentId)
        {
            var moved = item.Move(item.SpaceId, null, Array.Empty<Guid>(), 1, now);
            if (moved.IsFailure)
                return Result.Failure<DriveItemResponse>(moved.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DriveItemResponse.From(item));
    }
}

internal sealed class PurgeDriveItemCommandHandler : ICommandHandler<PurgeDriveItemCommand>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveAccessEvaluator _access;
    private readonly IDrivePurgeService _purge;
    private readonly IUserContext _userContext;

    public PurgeDriveItemCommandHandler(
        IDriveItemRepository items,
        IDriveAccessEvaluator access,
        IDrivePurgeService purge,
        IUserContext userContext)
    {
        _items = items;
        _access = access;
        _purge = purge;
        _userContext = userContext;
    }

    public async Task<Result> Handle(PurgeDriveItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure(DomainErrors.Drive.ItemNotFound);
        if (!item.IsTrashed)
            return Result.Failure(DomainErrors.Drive.ItemNotTrashed);

        var isHeadWithManage =
            await _access.IsDepartmentHeadOfAsync(_userContext.UserId, item.OwnerUserId, cancellationToken) &&
            await _access.CanAccessItemAsync(request.ItemId, DriveRole.ManagerId, cancellationToken);
        if (!_access.IsAdmin && !isHeadWithManage)
            return Result.Failure(DomainErrors.Drive.AccessDenied);

        try
        {
            await _purge.PurgeTreeAsync(request.ItemId, _userContext.UserId, "ManualPurge", cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure(DomainErrors.Drive.StorageNotConfigured);
        }

        return Result.Success();
    }
}

public sealed class RestoreDriveItemCommandValidator : AbstractValidator<RestoreDriveItemCommand>
{
    public RestoreDriveItemCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
    }
}

public sealed class PurgeDriveItemCommandValidator : AbstractValidator<PurgeDriveItemCommand>
{
    public PurgeDriveItemCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
    }
}