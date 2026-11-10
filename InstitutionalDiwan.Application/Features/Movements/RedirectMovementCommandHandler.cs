using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Features.DepartmentRecords;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Movements;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class RedirectMovementCommandHandler
    : ICommandHandler<RedirectMovementCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUserRepository _userRepository;
    private readonly CentralDiwanRouting _centralDiwanRouting;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationPublisher _notificationPublisher;
    private readonly DepartmentRecordService _recordService;

    public RedirectMovementCommandHandler(
        IDocumentMovementRepository movementRepository,
        IDepartmentRepository departmentRepository,
        IUserRepository userRepository,
        CentralDiwanRouting centralDiwanRouting,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUnitOfWork unitOfWork,
        INotificationPublisher notificationPublisher,
        DepartmentRecordService recordService)
    {
        _recordService = recordService;
        _movementRepository = movementRepository;
        _departmentRepository = departmentRepository;
        _userRepository = userRepository;
        _centralDiwanRouting = centralDiwanRouting;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _unitOfWork = unitOfWork;
        _notificationPublisher = notificationPublisher;
    }

    public async Task<Result<MovementResponse>> Handle(
        RedirectMovementCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        // A movement not sent yet is still with the sender, so custody is the sender's until then.
        var custody = movement.StatusId == DocumentMovement.StatusDraft
            ? await MovementCustody.EnsureSenderAsync(movement, _departmentScopeEvaluator, cancellationToken)
            : await MovementCustody.EnsureReceiverAsync(movement, _departmentScopeEvaluator, cancellationToken);
        if (custody.IsFailure)
            return Result.Failure<MovementResponse>(custody.Error);

        var destination = await _departmentRepository.GetByIdAsync(
            request.NewDestinationDepartmentId, cancellationToken);
        if (destination is null)
            return Result.Failure<MovementResponse>(DomainErrors.Department.NotFound);

        if (!destination.IsActive)
            return Result.Failure<MovementResponse>(DomainErrors.Department.Inactive);

        var routeResult = await _centralDiwanRouting.ValidateRouteAsync(
            movement.DestinationDepartmentId, destination.DepartmentId, cancellationToken);
        if (routeResult.IsFailure)
            return Result.Failure<MovementResponse>(routeResult.Error);

        if (request.AssignedToUserId is { } assignedToUserId)
        {
            var assignee = await _userRepository.GetByIdAsync(assignedToUserId, cancellationToken);
            if (assignee is null)
                return Result.Failure<MovementResponse>(DomainErrors.Identity.UserNotFound);
        }

        var leftStep = movement.Steps.Single(step => step.StepNo == movement.CurrentStepNo);
        var wasDraft = movement.StatusId == DocumentMovement.StatusDraft;
        var redirect = movement.RedirectTo(
            destination.DepartmentId,
            request.UserId,
            request.AssignedToUserId,
            request.Instructions,
            request.DueDate,
            _unitOfWork.UtcNow);
        if (redirect.IsFailure)
            return Result.Failure<MovementResponse>(redirect.Error);

        // The document leaves whoever held it: the sender of an unsent movement, otherwise the
        // department that received the step (a step redirected before receipt has no record).
        if (wasDraft)
            await _recordService.LockDepartmentAsync(movement.DocumentId, leftStep.FromDepartmentId, _unitOfWork.UtcNow, cancellationToken);
        else
            await _recordService.LockStepVisitAsync(movement.DocumentId, leftStep, _unitOfWork.UtcNow, cancellationToken);

        await _notificationPublisher.PublishMovementAsync(NotificationType.MovementRedirectedId, movement, request.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
