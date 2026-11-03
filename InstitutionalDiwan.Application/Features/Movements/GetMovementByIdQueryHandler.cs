using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class GetMovementByIdQueryHandler
    : IQueryHandler<GetMovementByIdQuery, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public GetMovementByIdQueryHandler(
        IDocumentMovementRepository movementRepository,
        IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _movementRepository = movementRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<MovementResponse>> Handle(
        GetMovementByIdQuery request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                movement.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<MovementResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        return MovementResponse.From(movement);
    }
}
