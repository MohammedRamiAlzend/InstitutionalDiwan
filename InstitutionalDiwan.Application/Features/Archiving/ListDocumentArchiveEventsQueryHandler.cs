using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Archiving;

internal sealed class ListDocumentArchiveEventsQueryHandler
    : IQueryHandler<ListDocumentArchiveEventsQuery, IReadOnlyCollection<DocumentArchiveEventResponse>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentArchiveRepository _documentArchiveRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public ListDocumentArchiveEventsQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentArchiveRepository documentArchiveRepository,
        IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _documentRepository = documentRepository;
        _documentArchiveRepository = documentArchiveRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<IReadOnlyCollection<DocumentArchiveEventResponse>>> Handle(
        ListDocumentArchiveEventsQuery request,
        CancellationToken cancellationToken)
    {
        if (await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken) is null)
        {
            return Result.Failure<IReadOnlyCollection<DocumentArchiveEventResponse>>(
                DomainErrors.Correspondence.DocumentNotFound);
        }

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<IReadOnlyCollection<DocumentArchiveEventResponse>>(
                DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var events = await _documentArchiveRepository.ListByDocumentAsync(
            request.DocumentId, cancellationToken);

        IReadOnlyCollection<DocumentArchiveEventResponse> responses =
            events.Select(DocumentArchiveEventResponse.From).ToList();

        return Result.Success(responses);
    }
}
