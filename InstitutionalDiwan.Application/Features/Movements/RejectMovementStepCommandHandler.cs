using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class RejectMovementStepCommandHandler
    : ICommandHandler<RejectMovementStepCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectMovementStepCommandHandler(
        IDocumentMovementRepository movementRepository,
        IUnitOfWork unitOfWork)
    {
        _movementRepository = movementRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MovementResponse>> Handle(
        RejectMovementStepCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        var reject = movement.RejectStep(request.UserId, request.Reason);
        if (reject.IsFailure)
            return Result.Failure<MovementResponse>(reject.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
