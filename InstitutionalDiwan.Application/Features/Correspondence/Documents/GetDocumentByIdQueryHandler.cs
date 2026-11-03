using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class GetDocumentByIdQueryHandler : IQueryHandler<GetDocumentByIdQuery, DocumentResponse>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;

    public GetDocumentByIdQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentAccessEvaluator documentAccessEvaluator)
    {
        _documentRepository = documentRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
    }

    public async Task<Result<DocumentResponse>> Handle(
        GetDocumentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<DocumentResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        return DocumentResponse.From(document);
    }
}
