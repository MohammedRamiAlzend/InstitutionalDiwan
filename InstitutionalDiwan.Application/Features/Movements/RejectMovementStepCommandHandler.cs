using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class RejectMovementStepCommandHandler
    : ICommandHandler<RejectMovementStepCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly CentralDiwanRouting _centralDiwanRouting;
    private readonly IUnitOfWork _unitOfWork;

    public RejectMovementStepCommandHandler(
        IDocumentMovementRepository movementRepository,
        CentralDiwanRouting centralDiwanRouting,
        IUnitOfWork unitOfWork)
    {
        _movementRepository = movementRepository;
        _centralDiwanRouting = centralDiwanRouting;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MovementResponse>> Handle(
        RejectMovementStepCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        var stepGuard = MovementStepGuard.EnsureCurrent(movement, request.StepId);
        if (stepGuard.IsFailure)
            return Result.Failure<MovementResponse>(stepGuard.Error);

        var centralResult = await _centralDiwanRouting.GetCentralDiwanDepartmentIdAsync(
            cancellationToken);
        if (centralResult.IsFailure)
            return Result.Failure<MovementResponse>(centralResult.Error);

        var reject = movement.RejectStep(
            request.UserId,
            request.Reason,
            centralResult.Value,
            _unitOfWork.UtcNow);
        if (reject.IsFailure)
            return Result.Failure<MovementResponse>(reject.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
