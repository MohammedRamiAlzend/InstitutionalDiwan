using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class GetDocumentDetailsQueryHandler
    : IQueryHandler<GetDocumentDetailsQuery, DocumentDetailResponse>
{
    private readonly IDocumentRepository _documentRepository;

    public GetDocumentDetailsQueryHandler(IDocumentRepository documentRepository) =>
        _documentRepository = documentRepository;

    public async Task<Result<DocumentDetailResponse>> Handle(
        GetDocumentDetailsQuery request,
        CancellationToken cancellationToken)
    {
        var document = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (document is null)
            return Result.Failure<DocumentDetailResponse>(
                DomainErrors.Correspondence.DocumentNotFound);

        return DocumentDetailResponse.From(document);
    }
}
