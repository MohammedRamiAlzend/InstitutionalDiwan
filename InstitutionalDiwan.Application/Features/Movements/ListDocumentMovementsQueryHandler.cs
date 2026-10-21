using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class ListDocumentMovementsQueryHandler
    : IQueryHandler<ListDocumentMovementsQuery, IReadOnlyCollection<MovementResponse>>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDocumentRepository _documentRepository;

    public ListDocumentMovementsQueryHandler(
        IDocumentMovementRepository movementRepository,
        IDocumentRepository documentRepository)
    {
        _movementRepository = movementRepository;
        _documentRepository = documentRepository;
    }

    public async Task<Result<IReadOnlyCollection<MovementResponse>>> Handle(
        ListDocumentMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<IReadOnlyCollection<MovementResponse>>(
                DomainErrors.Correspondence.DocumentNotFound);

        var movements = await _movementRepository.ListByDocumentAsync(
            request.DocumentId, cancellationToken);

        return Result.Success<IReadOnlyCollection<MovementResponse>>(
            movements.Select(MovementResponse.From).ToList());
    }
}
