using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Archiving;

internal sealed class SearchArchivedDocumentsQueryHandler
    : IQueryHandler<SearchArchivedDocumentsQuery, PagedResult<ArchivedDocumentResponse>>
{
    private readonly IDocumentArchiveRepository _documentArchiveRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public SearchArchivedDocumentsQueryHandler(
        IDocumentArchiveRepository documentArchiveRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUserContext userContext,
        IClock clock)
    {
        _documentArchiveRepository = documentArchiveRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _userContext = userContext;
        _clock = clock;
    }

    public async Task<Result<PagedResult<ArchivedDocumentResponse>>> Handle(
        SearchArchivedDocumentsQuery request,
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

        var rows = await _documentArchiveRepository.SearchAsync(
            new ArchiveSearchFilter(
                _userContext.UserId,
                accessibleDepartmentIds,
                _clock.UtcNow,
                request.OwningDepartmentId,
                request.DocumentTypeId,
                request.DirectionId,
                request.ArchiveLocationId,
                request.ArchivedFromUtc,
                request.ArchivedToUtc),
            page,
            cancellationToken);

        return Result.Success(new PagedResult<ArchivedDocumentResponse>(
            rows.Items.Select(ArchivedDocumentResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            rows.TotalCount));
    }
}
