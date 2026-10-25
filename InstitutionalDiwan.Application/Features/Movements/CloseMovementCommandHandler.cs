using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class CloseMovementCommandHandler
    : ICommandHandler<CloseMovementCommand, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CloseMovementCommandHandler(
        IDocumentMovementRepository movementRepository,
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork)
    {
        _movementRepository = movementRepository;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MovementResponse>> Handle(
        CloseMovementCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        var close = movement.Close(request.UserId, request.Notes, _unitOfWork.UtcNow);
        if (close.IsFailure)
            return Result.Failure<MovementResponse>(close.Error);

        // The document leaves the IN_MOVEMENT status when its movement ends, so it can be
        // archived or routed again.
        var document = await _documentRepository.GetByIdAsync(movement.DocumentId, cancellationToken);
        if (document is not null)
        {
            var released = document.MarkMovementEnded(
                request.UserId, request.Notes ?? "Movement closed", _unitOfWork.UtcNow);
            if (released.IsFailure)
                return Result.Failure<MovementResponse>(released.Error);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MovementResponse.From(movement);
    }
}
