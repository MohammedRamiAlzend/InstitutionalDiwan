using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Identity.AccessGrants;

internal sealed class ListDocumentAccessGrantsQueryHandler
    : IQueryHandler<ListDocumentAccessGrantsQuery, IReadOnlyCollection<DocumentAccessGrantResponse>>
{
    private readonly IDocumentAccessGrantRepository _grantRepository;

    public ListDocumentAccessGrantsQueryHandler(IDocumentAccessGrantRepository grantRepository) =>
        _grantRepository = grantRepository;

    public async Task<Result<IReadOnlyCollection<DocumentAccessGrantResponse>>> Handle(
        ListDocumentAccessGrantsQuery request,
        CancellationToken cancellationToken)
    {
        var grants = await _grantRepository.ListByDocumentAsync(request.DocumentId, cancellationToken);
        IReadOnlyCollection<DocumentAccessGrantResponse> responses =
            grants.Select(DocumentAccessGrantResponse.From).ToList();

        return Result.Success(responses);
    }
}
