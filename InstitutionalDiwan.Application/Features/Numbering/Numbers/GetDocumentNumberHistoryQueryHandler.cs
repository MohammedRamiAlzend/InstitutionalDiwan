using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

internal sealed class GetDocumentNumberHistoryQueryHandler
    : IQueryHandler<GetDocumentNumberHistoryQuery, IReadOnlyCollection<DocumentNumberResponse>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;

    public GetDocumentNumberHistoryQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentNumberRepository documentNumberRepository)
    {
        _documentRepository = documentRepository;
        _documentNumberRepository = documentNumberRepository;
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

        var numbers = await _documentNumberRepository.ListByDocumentAsync(
            request.DocumentId, cancellationToken);

        IReadOnlyCollection<DocumentNumberResponse> history = numbers
            .Select(DocumentNumberResponse.From)
            .ToList();

        return Result.Success(history);
    }
}
