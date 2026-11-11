using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Relations;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Relations;

internal sealed class CreateDocumentRelationCommandHandler
    : ICommandHandler<CreateDocumentRelationCommand, DocumentRelationResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentRelationRepository _relationRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDocumentRelationCommandHandler(
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

    public async Task<Result<DocumentRelationResponse>> Handle(
        CreateDocumentRelationCommand request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentRelationResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.DownloadId, cancellationToken))
        {
            return Result.Failure<DocumentRelationResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var notArchived = document.EnsureNotArchived();
        if (notArchived.IsFailure)
            return Result.Failure<DocumentRelationResponse>(notArchived.Error);

        var target = await _documentRepository.GetByIdAsync(request.TargetDocumentId, cancellationToken);
        if (target is null)
            return Result.Failure<DocumentRelationResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.TargetDocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<DocumentRelationResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        if (IsCancelled(document) || IsCancelled(target))
            return Result.Failure<DocumentRelationResponse>(DomainErrors.Relations.DocumentCancelled);

        var type = await _relationRepository.GetTypeByIdAsync(request.RelationTypeId, cancellationToken);
        if (type is null)
            return Result.Failure<DocumentRelationResponse>(DomainErrors.Relations.RelationTypeNotFound);

        if (!type.IsActive)
            return Result.Failure<DocumentRelationResponse>(DomainErrors.Relations.RelationTypeInactive);

        var utcNow = _unitOfWork.UtcNow;
        var relationResult = await DocumentRelationLinking.LinkAsync(
            _relationRepository,
            type,
            request.DocumentId,
            request.TargetDocumentId,
            request.UserId,
            request.Notes,
            utcNow,
            cancellationToken);
        if (relationResult.IsFailure)
            return Result.Failure<DocumentRelationResponse>(relationResult.Error);

        var relation = relationResult.Value;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return DocumentRelationResponse.From(relation, type, target, request.DocumentId);
    }

    private static bool IsCancelled(Document document) =>
        document.CurrentStatusId == DocumentStatusType.CanceledId;
}
