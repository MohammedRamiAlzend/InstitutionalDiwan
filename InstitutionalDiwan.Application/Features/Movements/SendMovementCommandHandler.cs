using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.DepartmentRecords;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class SendMovementCommandHandler
    : ICommandHandler<SendMovementCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly CentralDiwanRouting _centralDiwanRouting;
    private readonly ArchiveDiwanRouting _archiveDiwanRouting;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly DepartmentRecordService _recordService;

    public SendMovementCommandHandler(
        IDocumentMovementRepository movementRepository,
        CentralDiwanRouting centralDiwanRouting,
        ArchiveDiwanRouting archiveDiwanRouting,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUnitOfWork unitOfWork,
        INotificationPublisher notificationPublisher,
        DepartmentRecordService recordService)
    {
        _recordService = recordService;
        _movementRepository = movementRepository;
        _centralDiwanRouting = centralDiwanRouting;
        _archiveDiwanRouting = archiveDiwanRouting;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _unitOfWork = unitOfWork;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<MovementResponse>> Handle(
        SendMovementCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        var custody = await MovementCustody.EnsureSenderAsync(
            movement, _departmentScopeEvaluator, cancellationToken);
        if (custody.IsFailure)
            return Result.Failure<MovementResponse>(custody.Error);

        var routeResult = await _centralDiwanRouting.ValidateRouteAsync(
            movement.SourceDepartmentId, movement.DestinationDepartmentId, cancellationToken);
        if (routeResult.IsFailure)
            return Result.Failure<MovementResponse>(routeResult.Error);

        // The archive diwan may have been designated after this movement was drafted.
        var archiveRoute = await _archiveDiwanRouting.ValidateFirstHopAsync(
            movement.DocumentId,
            movement.MovementTypeId,
            movement.SourceDepartmentId,
            movement.DestinationDepartmentId,
            cancellationToken);
        if (archiveRoute.IsFailure)
            return Result.Failure<MovementResponse>(archiveRoute.Error);

        var leaving = movement.Steps.Single(step => step.StepNo == movement.CurrentStepNo).FromDepartmentId;
        var send = movement.Send(request.UserId, request.Notes, _unitOfWork.UtcNow);
        if (send.IsFailure)
            return Result.Failure<MovementResponse>(send.Error);

        // The document leaves the sending department, so its record is locked.
        await _recordService.LockDepartmentAsync(movement.DocumentId, leaving, _unitOfWork.UtcNow, cancellationToken);

        await _notificationPublisher.PublishMovementAsync(NotificationType.MovementSentId, movement, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
