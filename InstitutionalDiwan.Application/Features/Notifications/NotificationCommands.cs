using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Notifications;

/// <summary>Marks one inbox row of the calling user as read (idempotent).</summary>
public sealed record MarkNotificationReadCommand(Guid RecipientId, Guid UserId) : ICommand;

/// <summary>Hides one inbox row of the calling user (idempotent).</summary>
public sealed record DismissNotificationCommand(Guid RecipientId, Guid UserId) : ICommand;

/// <summary>Marks every unread notification of the calling user as read.</summary>
public sealed record MarkAllNotificationsReadCommand(Guid UserId) : ICommand<MarkedReadResponse>;

// Another user row is answered exactly like a missing one, so recipient ids cannot be probed.
internal sealed class MarkNotificationReadCommandHandler : ICommandHandler<MarkNotificationReadCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkNotificationReadCommandHandler(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var recipient = await _notificationRepository.GetRecipientAsync(
            request.RecipientId, request.UserId, cancellationToken);
        if (recipient is null)
            return Result.Failure(DomainErrors.Notifications.RecipientNotFound);

        var read = recipient.MarkAsRead(_unitOfWork.UtcNow);
        if (read.IsFailure)
            return read;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class DismissNotificationCommandHandler : ICommandHandler<DismissNotificationCommand>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DismissNotificationCommandHandler(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DismissNotificationCommand request, CancellationToken cancellationToken)
    {
        var recipient = await _notificationRepository.GetRecipientAsync(
            request.RecipientId, request.UserId, cancellationToken);
        if (recipient is null)
            return Result.Failure(DomainErrors.Notifications.RecipientNotFound);

        var dismissed = recipient.Dismiss();
        if (dismissed.IsFailure)
            return dismissed;

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal sealed class MarkAllNotificationsReadCommandHandler
    : ICommandHandler<MarkAllNotificationsReadCommand, MarkedReadResponse>
{
    private readonly INotificationRepository _notificationRepository;
    private readonly IUnitOfWork _unitOfWork;

    public MarkAllNotificationsReadCommandHandler(INotificationRepository notificationRepository, IUnitOfWork unitOfWork)
    {
        _notificationRepository = notificationRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MarkedReadResponse>> Handle(
        MarkAllNotificationsReadCommand request,
        CancellationToken cancellationToken)
    {
        var changed = await _notificationRepository.MarkAllReadAsync(
            request.UserId, _unitOfWork.UtcNow, cancellationToken);

        return Result.Success(new MarkedReadResponse(changed));
    }
}
