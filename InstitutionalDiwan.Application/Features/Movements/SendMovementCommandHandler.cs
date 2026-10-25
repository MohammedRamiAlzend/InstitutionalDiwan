using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class SendMovementCommandHandler
    : ICommandHandler<SendMovementCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly CentralDiwanRouting _centralDiwanRouting;
    private readonly IUnitOfWork _unitOfWork;

    public SendMovementCommandHandler(
        IDocumentMovementRepository movementRepository,
        CentralDiwanRouting centralDiwanRouting,
        IUnitOfWork unitOfWork)
    {
        _movementRepository = movementRepository;
        _centralDiwanRouting = centralDiwanRouting;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MovementResponse>> Handle(
        SendMovementCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        var routeResult = await _centralDiwanRouting.ValidateRouteAsync(
            movement.SourceDepartmentId, movement.DestinationDepartmentId, cancellationToken);
        if (routeResult.IsFailure)
            return Result.Failure<MovementResponse>(routeResult.Error);

        var send = movement.Send(request.UserId, request.Notes, _unitOfWork.UtcNow);
        if (send.IsFailure)
            return Result.Failure<MovementResponse>(send.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
