using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class ReceiveDocumentCommandHandler
    : ICommandHandler<ReceiveDocumentCommand, MovementStepReceiptResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ReceiveDocumentCommandHandler(
        IDocumentMovementRepository movementRepository,
        IDocumentRepository documentRepository,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _movementRepository = movementRepository;
        _documentRepository = documentRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<MovementStepReceiptResponse>> Handle(
        ReceiveDocumentCommand request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementStepReceiptResponse>(DomainErrors.Movements.MovementNotFound);

        var document = await _documentRepository.GetWithNumbersAsync(movement.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<MovementStepReceiptResponse>(DomainErrors.Correspondence.DocumentNotFound);

        var entry = document.FindDepartmentNumber(movement.DestinationDepartmentId);
        if (entry is null)
        {
            return Result.Failure<MovementStepReceiptResponse>(
                DomainErrors.Numbering.DepartmentRegistrationRequired);
        }

        var receive = movement.ReceiveStep(
            request.UserId, entry.DocumentNumberId, request.Notes, _unitOfWork.UtcNow);
        if (receive.IsFailure)
            return Result.Failure<MovementStepReceiptResponse>(receive.Error);

        var step = movement.Steps.Single(current => current.StepNo == movement.CurrentStepNo);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new MovementStepReceiptResponse(
            movement.MovementId,
            step.StepNo,
            movement.DestinationDepartmentId,
            entry.DocumentNumberId,
            entry.OfficialNumberText,
            entry.RegistryDate,
            step.ReceivedAt ?? _clock.UtcNow));
    }
}
