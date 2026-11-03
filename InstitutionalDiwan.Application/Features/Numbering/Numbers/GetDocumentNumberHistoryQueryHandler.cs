using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

internal sealed class GetDocumentNumberHistoryQueryHandler
    : IQueryHandler<GetDocumentNumberHistoryQuery, IReadOnlyCollection<DocumentNumberResponse>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public GetDocumentNumberHistoryQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository,
        IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<IReadOnlyCollection<DocumentNumberResponse>>> Handle(
        GetDocumentNumberHistoryQuery request,
        CancellationToken cancellationToken)
    {
        if (await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken) is null)
        {
            return Result.Failure<IReadOnlyCollection<DocumentNumberResponse>>(
                DomainErrors.Correspondence.DocumentNotFound);
        }

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<IReadOnlyCollection<DocumentNumberResponse>>(
                DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var numbers = await _documentNumberRepository.ListByDocumentAsync(
            request.DocumentId, cancellationToken);

        IReadOnlyCollection<DocumentNumberResponse> history = numbers
            .Select(DocumentNumberResponse.From)
            .ToList();

        return Result.Success(history);
    }
}
