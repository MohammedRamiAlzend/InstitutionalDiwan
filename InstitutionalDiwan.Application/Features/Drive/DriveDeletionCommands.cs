using FluentValidation;
using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>Requests deletion of an item; the owner's department head (or ADMIN) is notified (§3/2).</summary>
public sealed record RequestDriveItemDeletionCommand(Guid ItemId, string Reason) : ICommand<DriveDeletionRequestResponse>;

/// <summary>Approves a pending request and moves the item to the trash. Reviewer only.</summary>
public sealed record ApproveDriveDeletionCommand(Guid RequestId, string? Note = null) : ICommand<DriveDeletionRequestResponse>;

/// <summary>Rejects a pending request. Reviewer only.</summary>
public sealed record RejectDriveDeletionCommand(Guid RequestId, string? Note = null) : ICommand<DriveDeletionRequestResponse>;

/// <summary>Cancels a pending request. Requester only.</summary>
public sealed record CancelDriveDeletionCommand(Guid RequestId) : ICommand<DriveDeletionRequestResponse>;

/// <summary>Trashes an item directly where the caller may (ADMIN, or department head with Manage) (§3/3).</summary>
public sealed record DeleteDriveItemDirectlyCommand(Guid ItemId) : ICommand<DriveItemResponse>;

internal sealed class RequestDriveItemDeletionCommandHandler : ICommandHandler<RequestDriveItemDeletionCommand, DriveDeletionRequestResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveDeletionRequestRepository _requests;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IDriveNotificationPublisher _notifications;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public RequestDriveItemDeletionCommandHandler(
        IDriveItemRepository items,
        IDriveDeletionRequestRepository requests,
        IDriveAccessEvaluator access,
        IUserRepository users,
        IRoleRepository roles,
        IDriveNotificationPublisher notifications,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _requests = requests;
        _access = access;
        _users = users;
        _roles = roles;
        _notifications = notifications;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveDeletionRequestResponse>> Handle(RequestDriveItemDeletionCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<DriveDeletionRequestResponse>(DomainErrors.Drive.ItemNotFound);
        if (item.IsTrashed)
            return Result.Failure<DriveDeletionRequestResponse>(DomainErrors.Drive.ItemAlreadyTrashed);
        if (!await _access.CanAccessItemAsync(request.ItemId, DriveRole.EditorId, cancellationToken))
            return Result.Failure<DriveDeletionRequestResponse>(DomainErrors.Drive.AccessDenied);

        var pending = await _requests.GetPendingByItemAsync(request.ItemId, cancellationToken);
        if (pending is not null)
            return Result.Failure<DriveDeletionRequestResponse>(DomainErrors.Drive.DeletionPending);

        var created = DriveDeletionRequest.Create(request.ItemId, _userContext.UserId, request.Reason, _unitOfWork.UtcNow);
        if (created.IsFailure)
            return Result.Failure<DriveDeletionRequestResponse>(created.Error);

        _requests.Add(created.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var recipients = await DriveDeletionApprovals.ResolveApproverRecipientsAsync(
            _access, _users, _roles, item.OwnerUserId, cancellationToken);
        await _notifications.PublishAsync(
            NotificationType.DriveDeletionRequestedId, recipients, item.Name, _userContext.UserId,
            _userContext.Username, $"/drive/deletion-requests/{created.Value.RequestId}", cancellationToken);

        return Result.Success(DriveDeletionRequestResponse.From(created.Value));
    }
}

internal sealed class ApproveDriveDeletionCommandHandler : ICommandHandler<ApproveDriveDeletionCommand, DriveDeletionRequestResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveDeletionRequestRepository _requests;
    private readonly IDriveAccessEvaluator _access;
    private readonly IDriveNotificationPublisher _notifications;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public ApproveDriveDeletionCommandHandler(
        IDriveItemRepository items,
        IDriveDeletionRequestRepository requests,
        IDriveAccessEvaluator access,
        IDriveNotificationPublisher notifications,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _requests = requests;
        _access = access;
        _notifications = notifications;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveDeletionRequestResponse>> Handle(ApproveDriveDeletionCommand request, CancellationToken cancellationToken)
    {
        var (review, item) = await LoadReviewableAsync(_requests, _items, _access, request.RequestId, _userContext.UserId, cancellationToken);
        if (review.IsFailure)
            return Result.Failure<DriveDeletionRequestResponse>(review.Error);
        var deletionRequest = review.Value;

        var approved = deletionRequest.Approve(_userContext.UserId, request.Note, _unitOfWork.UtcNow);
        if (approved.IsFailure)
            return Result.Failure<DriveDeletionRequestResponse>(approved.Error);

        var trashed = item.Trash(_userContext.UserId, _unitOfWork.UtcNow);
        if (trashed.IsFailure)
            return Result.Failure<DriveDeletionRequestResponse>(trashed.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifications.PublishAsync(
            NotificationType.DriveDeletionApprovedId, new[] { deletionRequest.RequestedByUserId }, item.Name,
            _userContext.UserId, null, $"/drive/items/{item.ItemId}", cancellationToken);

        return Result.Success(DriveDeletionRequestResponse.From(deletionRequest));
    }

    internal static async Task<(Result<DriveDeletionRequest> Request, DriveItem Item)> LoadReviewableAsync(
        IDriveDeletionRequestRepository requests,
        IDriveItemRepository items,
        IDriveAccessEvaluator access,
        Guid requestId,
        Guid reviewerUserId,
        CancellationToken cancellationToken)
    {
        var deletionRequest = await requests.GetByIdAsync(requestId, cancellationToken);
        if (deletionRequest is null)
            return (Result.Failure<DriveDeletionRequest>(DomainErrors.Drive.RequestNotFound), null!);

        var item = await items.GetByIdAsync(deletionRequest.ItemId, cancellationToken);
        if (item is null)
            return (Result.Failure<DriveDeletionRequest>(DomainErrors.Drive.ItemNotFound), null!);

        if (!await DriveDeletionApprovals.CanReviewAsync(access, reviewerUserId, item.OwnerUserId, cancellationToken))
            return (Result.Failure<DriveDeletionRequest>(DomainErrors.Drive.AccessDenied), null!);

        return (Result.Success(deletionRequest), item);
    }
}

internal sealed class RejectDriveDeletionCommandHandler : ICommandHandler<RejectDriveDeletionCommand, DriveDeletionRequestResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveDeletionRequestRepository _requests;
    private readonly IDriveAccessEvaluator _access;
    private readonly IDriveNotificationPublisher _notifications;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public RejectDriveDeletionCommandHandler(
        IDriveItemRepository items,
        IDriveDeletionRequestRepository requests,
        IDriveAccessEvaluator access,
        IDriveNotificationPublisher notifications,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _requests = requests;
        _access = access;
        _notifications = notifications;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveDeletionRequestResponse>> Handle(RejectDriveDeletionCommand request, CancellationToken cancellationToken)
    {
        var (review, item) = await ApproveDriveDeletionCommandHandler.LoadReviewableAsync(
            _requests, _items, _access, request.RequestId, _userContext.UserId, cancellationToken);
        if (review.IsFailure)
            return Result.Failure<DriveDeletionRequestResponse>(review.Error);
        var deletionRequest = review.Value;

        var rejected = deletionRequest.Reject(_userContext.UserId, request.Note, _unitOfWork.UtcNow);
        if (rejected.IsFailure)
            return Result.Failure<DriveDeletionRequestResponse>(rejected.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _notifications.PublishAsync(
            NotificationType.DriveDeletionRejectedId, new[] { deletionRequest.RequestedByUserId }, item.Name,
            _userContext.UserId, null, $"/drive/items/{item.ItemId}", cancellationToken);

        return Result.Success(DriveDeletionRequestResponse.From(deletionRequest));
    }
}

internal sealed class CancelDriveDeletionCommandHandler : ICommandHandler<CancelDriveDeletionCommand, DriveDeletionRequestResponse>
{
    private readonly IDriveDeletionRequestRepository _requests;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public CancelDriveDeletionCommandHandler(
        IDriveDeletionRequestRepository requests,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _requests = requests;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveDeletionRequestResponse>> Handle(CancelDriveDeletionCommand request, CancellationToken cancellationToken)
    {
        var deletionRequest = await _requests.GetByIdAsync(request.RequestId, cancellationToken);
        if (deletionRequest is null)
            return Result.Failure<DriveDeletionRequestResponse>(DomainErrors.Drive.RequestNotFound);

        var cancelled = deletionRequest.Cancel(_userContext.UserId);
        if (cancelled.IsFailure)
            return Result.Failure<DriveDeletionRequestResponse>(cancelled.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DriveDeletionRequestResponse.From(deletionRequest));
    }
}

internal sealed class DeleteDriveItemDirectlyCommandHandler : ICommandHandler<DeleteDriveItemDirectlyCommand, DriveItemResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteDriveItemDirectlyCommandHandler(
        IDriveItemRepository items,
        IDriveAccessEvaluator access,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _access = access;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveItemResponse>> Handle(DeleteDriveItemDirectlyCommand request, CancellationToken cancellationToken)
    {
        var item = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (item is null)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemNotFound);
        if (item.IsTrashed)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemAlreadyTrashed);

        var isHeadWithManage =
            await _access.IsDepartmentHeadOfAsync(_userContext.UserId, item.OwnerUserId, cancellationToken) &&
            await _access.CanAccessItemAsync(request.ItemId, DriveRole.ManagerId, cancellationToken);
        if (!_access.IsAdmin && !isHeadWithManage)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);

        var trashed = item.Trash(_userContext.UserId, _unitOfWork.UtcNow);
        if (trashed.IsFailure)
            return Result.Failure<DriveItemResponse>(trashed.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(DriveItemResponse.From(item));
    }
}

public sealed class RequestDriveItemDeletionCommandValidator : AbstractValidator<RequestDriveItemDeletionCommand>
{
    public RequestDriveItemDeletionCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
        RuleFor(command => command.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason)).WithErrorCode("Drive.RequestReasonEmpty")
            .MaximumLength(DriveDeletionRequest.MaxReasonLength).WithErrorCode("Drive.RequestReasonTooLong");
    }
}

public sealed class ApproveDriveDeletionCommandValidator : AbstractValidator<ApproveDriveDeletionCommand>
{
    public ApproveDriveDeletionCommandValidator()
    {
        RuleFor(command => command.RequestId).NotEmpty().WithErrorCode("Drive.RequestNotFound");
        RuleFor(command => command.Note)
            .MaximumLength(DriveDeletionRequest.MaxReviewNoteLength).WithErrorCode("Drive.ReviewNoteTooLong");
    }
}

public sealed class RejectDriveDeletionCommandValidator : AbstractValidator<RejectDriveDeletionCommand>
{
    public RejectDriveDeletionCommandValidator()
    {
        RuleFor(command => command.RequestId).NotEmpty().WithErrorCode("Drive.RequestNotFound");
        RuleFor(command => command.Note)
            .MaximumLength(DriveDeletionRequest.MaxReviewNoteLength).WithErrorCode("Drive.ReviewNoteTooLong");
    }
}

public sealed class CancelDriveDeletionCommandValidator : AbstractValidator<CancelDriveDeletionCommand>
{
    public CancelDriveDeletionCommandValidator()
    {
        RuleFor(command => command.RequestId).NotEmpty().WithErrorCode("Drive.RequestNotFound");
    }
}

public sealed class DeleteDriveItemDirectlyCommandValidator : AbstractValidator<DeleteDriveItemDirectlyCommand>
{
    public DeleteDriveItemDirectlyCommandValidator()
    {
        RuleFor(command => command.ItemId).NotEmpty().WithErrorCode("Drive.ItemNotFound");
    }
}