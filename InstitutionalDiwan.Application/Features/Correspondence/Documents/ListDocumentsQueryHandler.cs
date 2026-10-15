using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class ListDocumentsQueryHandler
    : IQueryHandler<ListDocumentsQuery, IReadOnlyCollection<DocumentResponse>>
{
    private const int DefaultLimit = 50;

    private readonly IDocumentRepository _documentRepository;

    public ListDocumentsQueryHandler(IDocumentRepository documentRepository) =>
        _documentRepository = documentRepository;

    public async Task<Result<IReadOnlyCollection<DocumentResponse>>> Handle(
        ListDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var documents = await _documentRepository.ListRecentAsync(DefaultLimit, cancellationToken);
        IReadOnlyCollection<DocumentResponse> responses = documents.Select(DocumentResponse.From).ToList();
        return Result.Success(responses);
    }
}
