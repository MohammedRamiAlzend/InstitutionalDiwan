using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.DepartmentRecords;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class RejectMovementStepCommandHandler
    : ICommandHandler<RejectMovementStepCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly CentralDiwanRouting _centralDiwanRouting;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly DepartmentRecordService _recordService;

    public RejectMovementStepCommandHandler(
        IDocumentMovementRepository movementRepository,
        CentralDiwanRouting centralDiwanRouting,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUnitOfWork unitOfWork,
        INotificationPublisher notificationPublisher,
        DepartmentRecordService recordService)
    {
        _recordService = recordService;
        _movementRepository = movementRepository;
        _centralDiwanRouting = centralDiwanRouting;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _unitOfWork = unitOfWork;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<MovementResponse>> Handle(
        RejectMovementStepCommand request,
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

        var centralResult = await _centralDiwanRouting.GetCentralDiwanDepartmentIdAsync(
            cancellationToken);
        if (centralResult.IsFailure)
            return Result.Failure<MovementResponse>(centralResult.Error);

        var rejectedStep = movement.Steps.Single(step => step.StepNo == movement.CurrentStepNo);
        var reject = movement.RejectStep(
            request.UserId,
            request.Reason,
            centralResult.Value,
            _unitOfWork.UtcNow);
        if (reject.IsFailure)
            return Result.Failure<MovementResponse>(reject.Error);

        // The rejecting department's record of this step is closed, whether the document goes on to the
        // Central Diwan or (when the Central Diwan rejects) stays there. A step rejected before receipt
        // has no record.
        await _recordService.LockStepVisitAsync(movement.DocumentId, rejectedStep, _unitOfWork.UtcNow, cancellationToken);

        await _notificationPublisher.PublishMovementAsync(NotificationType.MovementRejectedId, movement, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
