using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class CancelMovementCommandHandler
    : ICommandHandler<CancelMovementCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPublisher _notificationPublisher;

    public CancelMovementCommandHandler(
        IDocumentMovementRepository movementRepository,
        IDocumentRepository documentRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUnitOfWork unitOfWork,
        INotificationPublisher notificationPublisher)
    {
        _movementRepository = movementRepository;
        _documentRepository = documentRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _unitOfWork = unitOfWork;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<MovementResponse>> Handle(
        CancelMovementCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        var custody = await MovementCustody.EnsureSenderAsync(
            movement, _departmentScopeEvaluator, cancellationToken);
        if (custody.IsFailure)
            return Result.Failure<MovementResponse>(custody.Error);

        var cancel = movement.Cancel(request.UserId, request.Reason, _unitOfWork.UtcNow);
        if (cancel.IsFailure)
            return Result.Failure<MovementResponse>(cancel.Error);

        // The document leaves the IN_MOVEMENT status when its movement ends, so it can be
        // archived or routed again.
        var document = await _documentRepository.GetByIdAsync(movement.DocumentId, cancellationToken);
        if (document is not null)
        {
            var released = document.MarkMovementEnded(
                request.UserId, request.Reason, _unitOfWork.UtcNow);
            if (released.IsFailure)
                return Result.Failure<MovementResponse>(released.Error);
        }

        await _notificationPublisher.PublishMovementAsync(NotificationType.MovementCancelledId, movement, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
