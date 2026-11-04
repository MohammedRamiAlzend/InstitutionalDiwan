using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class ListDocumentsQueryHandler
    : IQueryHandler<ListDocumentsQuery, PagedResult<DocumentResponse>>
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentFieldRepository _documentFieldRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public ListDocumentsQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentFieldRepository documentFieldRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUserContext userContext,
        IClock clock)
    {
        _documentRepository = documentRepository;
        _documentFieldRepository = documentFieldRepository;
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

        IReadOnlyList<DocumentFieldFilter>? fieldFilters = null;
        if (request.FieldFilters is { Count: > 0 } rawFilters)
        {
            if (request.DocumentTypeId is not { } documentTypeId)
                return Result.Failure<PagedResult<DocumentResponse>>(
                    DomainErrors.DocumentFields.FilterRequiresDocumentType);

            var fields = await _documentFieldRepository.ListByDocumentTypeAsync(
                documentTypeId, cancellationToken);
            var parsed = DocumentFieldFilterParser.Parse(rawFilters, fields);
            if (parsed.IsFailure)
                return Result.Failure<PagedResult<DocumentResponse>>(parsed.Error);

            fieldFilters = parsed.Value;
        }

        var accessibleDepartmentIds = await _departmentScopeEvaluator.GetAccessibleDepartmentIdsAsync(
            cancellationToken);

        var documents = await _documentRepository.ListPagedAsync(
            page,
            new DocumentViewAccess(_userContext.UserId, accessibleDepartmentIds, _clock.UtcNow),
            request.StatusId,
            request.OwningDepartmentId,
            request.DirectionId,
            request.DocumentTypeId,
            fieldFilters,
            cancellationToken);

        return Result.Success(new PagedResult<DocumentResponse>(
            documents.Items.Select(DocumentResponse.From).ToList(),
            page.NormalizedPage,
            page.NormalizedPageSize,
            documents.TotalCount));
    }
}
