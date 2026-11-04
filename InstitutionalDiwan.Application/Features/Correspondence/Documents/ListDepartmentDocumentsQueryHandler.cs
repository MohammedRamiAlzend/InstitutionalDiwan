using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

/// <summary>
/// Paged read of one department's documents. The department replaces the free-form
/// <c>owningDepartmentId</c> filter of <see cref="ListDocumentsQuery"/>, so the endpoint can be
/// addressed as that department's register and authorized against the caller's scope. Scope opens
/// the register; the rows are still limited to the documents the caller may view.
/// </summary>
internal sealed class ListDepartmentDocumentsQueryHandler
    : IQueryHandler<ListDepartmentDocumentsQuery, PagedResult<DocumentResponse>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public ListDepartmentDocumentsQueryHandler(
        IDocumentRepository documentRepository,
        IDepartmentRepository departmentRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUserContext userContext,
        IClock clock)
    {
        _documentRepository = documentRepository;
        _departmentRepository = departmentRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _userContext = userContext;
        _clock = clock;
    }

    public async Task<Result<PagedResult<DocumentResponse>>> Handle(
        ListDepartmentDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        var department = await _departmentRepository.GetByIdAsync(
            request.DepartmentId, cancellationToken);

        if (department is null)
            return Result.Failure<PagedResult<DocumentResponse>>(DomainErrors.Department.NotFound);

        var canAccess = await _departmentScopeEvaluator.CanAccessDepartmentAsync(
            request.DepartmentId, cancellationToken);

        if (!canAccess)
            return Result.Failure<PagedResult<DocumentResponse>>(
                DomainErrors.Correspondence.DepartmentOutOfScope);

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
            request.DepartmentId,
            request.DirectionId,
            request.DocumentTypeId,
            fieldFilters: null,
            cancellationToken: cancellationToken);

        return Result.Success(new PagedResult<DocumentResponse>(
            documents.Items.Select(DocumentResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            documents.TotalCount));
    }
}
