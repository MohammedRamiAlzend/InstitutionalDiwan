using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class ReturnMovementToSenderCommandHandler
    : ICommandHandler<ReturnMovementToSenderCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ReturnMovementToSenderCommandHandler(
        IDocumentMovementRepository movementRepository,
        IUnitOfWork unitOfWork)
    {
        _movementRepository = movementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MovementResponse>> Handle(
        ReturnMovementToSenderCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        var stepGuard = MovementStepGuard.EnsureCurrent(movement, request.StepId);
        if (stepGuard.IsFailure)
            return Result.Failure<MovementResponse>(stepGuard.Error);

        var returned = movement.ReturnToSender(request.UserId, request.Reason, _unitOfWork.UtcNow);
        if (returned.IsFailure)
            return Result.Failure<MovementResponse>(returned.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
