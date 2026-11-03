using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Relations;

internal sealed class ListDocumentRelationEventsQueryHandler
    : IQueryHandler<ListDocumentRelationEventsQuery, PagedResult<DocumentRelationEventResponse>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentRelationRepository _relationRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public ListDocumentRelationEventsQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentRelationRepository relationRepository,
        IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _documentRepository = documentRepository;
        _relationRepository = relationRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<PagedResult<DocumentRelationEventResponse>>> Handle(
        ListDocumentRelationEventsQuery request,
        CancellationToken cancellationToken)
    {
        if (await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken) is null)
        {
            return Result.Failure<PagedResult<DocumentRelationEventResponse>>(
                DomainErrors.Correspondence.DocumentNotFound);
        }

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<PagedResult<DocumentRelationEventResponse>>(
                DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var page = new PageRequest(request.Page, request.PageSize);
        var events = await _relationRepository.ListEventsByDocumentAsync(
            request.DocumentId, page, cancellationToken);

        return Result.Success(new PagedResult<DocumentRelationEventResponse>(
            events.Items.Select(DocumentRelationEventResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            events.TotalCount));
    }
}
