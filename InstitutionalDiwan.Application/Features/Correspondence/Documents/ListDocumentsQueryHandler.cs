using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class ListDocumentsQueryHandler
    : IQueryHandler<ListDocumentsQuery, PagedResult<DocumentResponse>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public ListDocumentsQueryHandler(
        IDocumentRepository documentRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUserContext userContext,
        IClock clock)
    {
        _documentRepository = documentRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _userContext = userContext;
        _clock = clock;
    }

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

        var accessibleDepartmentIds = await _departmentScopeEvaluator.GetAccessibleDepartmentIdsAsync(
            cancellationToken);

        var documents = await _documentRepository.ListPagedAsync(
            page,
            new DocumentViewAccess(_userContext.UserId, accessibleDepartmentIds, _clock.UtcNow),
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
