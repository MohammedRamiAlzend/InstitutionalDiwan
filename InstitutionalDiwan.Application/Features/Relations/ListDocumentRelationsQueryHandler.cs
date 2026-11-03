using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Relations;

internal sealed class ListDocumentRelationsQueryHandler
    : IQueryHandler<ListDocumentRelationsQuery, DocumentRelationsResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentRelationRepository _relationRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public ListDocumentRelationsQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentRelationRepository relationRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUserContext userContext,
        IClock clock)
    {
        _documentRepository = documentRepository;
        _relationRepository = relationRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _userContext = userContext;
        _clock = clock;
    }

    public async Task<Result<DocumentRelationsResponse>> Handle(
        ListDocumentRelationsQuery request,
        CancellationToken cancellationToken)
    {
        if (await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken) is null)
            return Result.Failure<DocumentRelationsResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<DocumentRelationsResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var relations = await _relationRepository.GetByDocumentIdAsync(request.DocumentId, cancellationToken);
        if (relations.Count == 0)
            return new DocumentRelationsResponse(Array.Empty<DocumentRelationResponse>(), 0);

        var otherIds = relations
            .Select(relation => relation.SourceDocumentId == request.DocumentId
                ? relation.TargetDocumentId
                : relation.SourceDocumentId)
            .Distinct()
            .ToList();

        var access = new DocumentViewAccess(
            _userContext.UserId,
            await _departmentScopeEvaluator.GetAccessibleDepartmentIdsAsync(cancellationToken),
            _clock.UtcNow);
        var viewableIds = await _documentRepository.ListViewableIdsAsync(otherIds, access, cancellationToken);

        var documents = (await _documentRepository.GetByIdsAsync(viewableIds.ToList(), cancellationToken))
            .ToDictionary(document => document.DocumentId);
        var types = (await _relationRepository.ListTypesAsync(
                relations.Select(relation => relation.RelationTypeId).Distinct().ToList(), cancellationToken))
            .ToDictionary(type => type.Id);

        var items = new List<DocumentRelationResponse>();
        var hidden = 0;
        foreach (var relation in relations)
        {
            var otherId = relation.SourceDocumentId == request.DocumentId
                ? relation.TargetDocumentId
                : relation.SourceDocumentId;

            if (!documents.TryGetValue(otherId, out var other) || !types.TryGetValue(relation.RelationTypeId, out var type))
            {
                hidden++;
                continue;
            }

            items.Add(DocumentRelationResponse.From(relation, type, other, request.DocumentId));
        }

        return new DocumentRelationsResponse(items, hidden);
    }
}
