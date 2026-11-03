using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Relations;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Relations;

internal sealed class RemoveDocumentRelationCommandHandler : ICommandHandler<RemoveDocumentRelationCommand>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentRelationRepository _relationRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveDocumentRelationCommandHandler(
        IDocumentRepository documentRepository,
        IDocumentRelationRepository relationRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IUnitOfWork unitOfWork)
    {
        _documentRepository = documentRepository;
        _relationRepository = relationRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveDocumentRelationCommand request, CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.DownloadId, cancellationToken))
        {
            return Result.Failure(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var notArchived = document.EnsureNotArchived();
        if (notArchived.IsFailure)
            return notArchived;

        // A link that does not touch this document answers as not found, never as another document's link.
        var relation = await _relationRepository.GetByIdAsync(request.RelationId, cancellationToken);
        if (relation is null ||
            (relation.SourceDocumentId != request.DocumentId && relation.TargetDocumentId != request.DocumentId))
        {
            return Result.Failure(DomainErrors.Relations.RelationNotFound);
        }

        var removed = relation.Remove(request.UserId, request.Reason);
        if (removed.IsFailure)
            return removed;

        var eventResult = DocumentRelationEvent.Record(
            relation.RelationId,
            relation.SourceDocumentId,
            relation.TargetDocumentId,
            relation.RelationTypeId,
            RelationActionType.RemovedId,
            request.UserId,
            request.Reason,
            _unitOfWork.UtcNow);
        if (eventResult.IsFailure)
            return Result.Failure(eventResult.Error);

        _relationRepository.Add(eventResult.Value);
        _relationRepository.Remove(relation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
