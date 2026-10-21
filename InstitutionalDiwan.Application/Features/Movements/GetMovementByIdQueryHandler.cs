using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class GetMovementByIdQueryHandler
    : IQueryHandler<GetMovementByIdQuery, MovementResponse>
{
    private readonly IDocumentMovementRepository _movementRepository;

    public GetMovementByIdQueryHandler(IDocumentMovementRepository movementRepository) =>
        _movementRepository = movementRepository;

    public async Task<Result<MovementResponse>> Handle(
        GetMovementByIdQuery request,
        CancellationToken cancellationToken)
    {
        var movement = await _movementRepository.GetByIdAsync(request.MovementId, cancellationToken);
        if (movement is null)
            return Result.Failure<MovementResponse>(DomainErrors.Movements.MovementNotFound);

        return MovementResponse.From(movement);
    }
}
