using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Movements;

internal sealed class ListDocumentMovementsQueryHandler
    : IQueryHandler<ListDocumentMovementsQuery, IReadOnlyCollection<MovementResponse>>
{
    private readonly IDocumentMovementRepository _movementRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public ListDocumentMovementsQueryHandler(
        IDocumentMovementRepository movementRepository,
        IDocumentRepository documentRepository,
        IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _movementRepository = movementRepository;
        _documentRepository = documentRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<IReadOnlyCollection<MovementResponse>>> Handle(
        ListDocumentMovementsQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<IReadOnlyCollection<MovementResponse>>(
                DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<IReadOnlyCollection<MovementResponse>>(
                DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var movements = await _movementRepository.ListByDocumentAsync(
            request.DocumentId, cancellationToken);

        return Result.Success<IReadOnlyCollection<MovementResponse>>(
            movements.Select(MovementResponse.From).ToList());
    }
}
