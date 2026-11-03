using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class ReturnMovementToSenderCommandHandler
    : ICommandHandler<ReturnMovementToSenderCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPublisher _notificationPublisher;

    public ReturnMovementToSenderCommandHandler(
        IDocumentMovementRepository movementRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUnitOfWork unitOfWork,
        INotificationPublisher notificationPublisher)
    {
        _movementRepository = movementRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _unitOfWork = unitOfWork;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<MovementResponse>> Handle(
        ReturnMovementToSenderCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        var custody = await MovementCustody.EnsureReceiverAsync(
            movement, _departmentScopeEvaluator, cancellationToken);
        if (custody.IsFailure)
            return Result.Failure<MovementResponse>(custody.Error);

        var stepGuard = MovementStepGuard.EnsureCurrent(movement, request.StepId);
        if (stepGuard.IsFailure)
            return Result.Failure<MovementResponse>(stepGuard.Error);

        var returned = movement.ReturnToSender(request.UserId, request.Reason, _unitOfWork.UtcNow);
        if (returned.IsFailure)
            return Result.Failure<MovementResponse>(returned.Error);

        await _notificationPublisher.PublishMovementAsync(NotificationType.MovementReturnedId, movement, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
