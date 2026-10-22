using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class ListDocumentsQueryHandler
    : IQueryHandler<ListDocumentsQuery, PagedResult<DocumentResponse>>
{
    private readonly IDocumentRepository _documentRepository;

    public ListDocumentsQueryHandler(IDocumentRepository documentRepository) =>
        _documentRepository = documentRepository;

    public async Task<Result<PagedResult<DocumentResponse>>> Handle(
        ListDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(
            request.Page,
            request.PageSize,
            request.Search,
            request.SortBy,
            request.SortDescending);

        var documents = await _documentRepository.ListPagedAsync(
            page,
            request.StatusId,
            request.OwningDepartmentId,
            request.DirectionId,
            request.DocumentTypeId,
            cancellationToken);

        return Result.Success(new PagedResult<DocumentResponse>(
            documents.Items.Select(DocumentResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            documents.TotalCount));
    }
}
