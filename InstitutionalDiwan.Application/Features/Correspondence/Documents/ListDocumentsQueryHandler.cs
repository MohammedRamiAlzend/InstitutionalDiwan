using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Correspondence.Documents;

internal sealed class ListDocumentsQueryHandler
    : IQueryHandler<ListDocumentsQuery, PagedResult<DocumentResponse>>
{
    private const string FieldSortPrefix = "field:";

    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentFieldRepository _documentFieldRepository;
    private readonly IDocumentTypeSchemaVersionRepository _schemaVersionRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public ListDocumentsQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentFieldRepository documentFieldRepository,
        IDocumentTypeSchemaVersionRepository schemaVersionRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUserContext userContext,
        IClock clock)
    {
        _documentRepository = documentRepository;
        _documentFieldRepository = documentFieldRepository;
        _schemaVersionRepository = schemaVersionRepository;
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

        var fieldFilters = await DocumentFieldFilterParser.ResolveAsync(
            request.FieldFilters, request.DocumentTypeId, _documentFieldRepository, cancellationToken);
        if (fieldFilters.IsFailure)
            return Result.Failure<PagedResult<DocumentResponse>>(fieldFilters.Error);

        var accessibleDepartmentIds = await _departmentScopeEvaluator.GetAccessibleDepartmentIdsAsync(
            cancellationToken);

        var documents = await _documentRepository.ListPagedAsync(
            page,
            new DocumentViewAccess(_userContext.UserId, accessibleDepartmentIds, _clock.UtcNow),
            request.StatusId,
            request.OwningDepartmentId,
            request.DirectionId,
            request.DocumentTypeId,
            fieldFilters.Value,
            await ResolveFieldSortAsync(request, cancellationToken),
            cancellationToken);

        return Result.Success(new PagedResult<DocumentResponse>(
            await WithColumnValuesAsync(documents.Items, request.DocumentTypeId, cancellationToken),
            page.NormalizedPage,
            page.NormalizedPageSize,
            documents.TotalCount));
    }

    /// <summary><c>sortBy=field:key</c> orders by a dynamic field; an unknown key keeps the default order.</summary>
    private async Task<DocumentFieldSort?> ResolveFieldSortAsync(
        ListDocumentsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.DocumentTypeId is not { } typeId
            || request.SortBy is not { } sortBy
            || !sortBy.StartsWith(FieldSortPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var key = sortBy[FieldSortPrefix.Length..];
        var field = await _documentFieldRepository.GetByKeyAsync(typeId, key, cancellationToken);
        return field?.DataType is DocumentFieldDataType.Text or DocumentFieldDataType.Number
            or DocumentFieldDataType.Date or DocumentFieldDataType.Boolean
            ? new DocumentFieldSort(field.DocumentFieldId, field.DataType)
            : null;
    }

    /// <summary>
    /// Adds the values of the columns the published schema shows. The rows are already limited to what
    /// the caller may view, and those fields are exactly what the document's details would show them.
    /// </summary>
    private async Task<IReadOnlyList<DocumentResponse>> WithColumnValuesAsync(
        IReadOnlyList<Document> documents,
        int? documentTypeId,
        CancellationToken cancellationToken)
    {
        var rows = documents.Select(DocumentResponse.From).ToList();
        if (documentTypeId is not { } typeId || rows.Count == 0)
            return rows;

        var published = await _schemaVersionRepository.GetPublishedAsync(typeId, cancellationToken);
        var columns = published?.Fields.Where(field => field.Behavior.ShowInTable).ToList();
        if (columns is not { Count: > 0 })
            return rows;

        var byFieldId = columns.ToDictionary(column => column.DocumentFieldId);
        var values = await _documentRepository.ListFieldValuesAsync(
            rows.Select(row => row.DocumentId).ToList(), byFieldId.Keys, cancellationToken);

        var byDocument = values
            .GroupBy(value => value.DocumentId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyDictionary<string, string?>)group.ToDictionary(
                    value => byFieldId[value.DocumentFieldId].FieldKey,
                    value => DocumentFieldValueText.Format(byFieldId[value.DocumentFieldId].DataType, value)));

        return rows
            .Select(row => byDocument.TryGetValue(row.DocumentId, out var fields) ? row with { FieldValues = fields } : row)
            .ToList();
    }
}
