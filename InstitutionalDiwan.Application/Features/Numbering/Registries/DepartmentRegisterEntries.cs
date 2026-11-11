using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Correspondence.DocumentFields;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;

namespace InstitutionalDiwan.Application.Features.Numbering.Registries;

/// <summary>
/// One page of a department's register lines. <paramref name="SchemaVersionId"/> picks a record form
/// version of the department: only lines with a visit on that version come back, each with that visit's
/// values. <paramref name="SortBy"/> is a column name (<c>registryNumber</c>, <c>registryDate</c>,
/// <c>document</c>, <c>centralNumber</c>, <c>numberingYear</c>, <c>status</c>, <c>registrationCount</c>)
/// or <c>field:{key}</c> for a field of the chosen version.
/// </summary>
public sealed record ListDepartmentRegisterEntriesQuery(
    Guid DepartmentId,
    Guid? RegistryId = null,
    int? NumberingYear = null,
    int? StatusId = null,
    DateTime? DateFrom = null,
    DateTime? DateTo = null,
    Guid? SchemaVersionId = null,
    int Page = 1,
    int PageSize = 20,
    string? Search = null,
    string? SortBy = null,
    bool SortDescending = true) : IQuery<PagedResult<DepartmentRegisterEntryResponse>>;

/// <summary>
/// A register line with the record values of its latest visit on the chosen form version. <c>Values</c>
/// is keyed by field key and holds the stored text of each field (see <c>DocumentFieldValueText</c>);
/// it is empty when no version was chosen. A line whose document the caller cannot view has an empty
/// title and no central number.
/// </summary>
public sealed record DepartmentRegisterEntryResponse(
    Guid DocumentNumberId,
    Guid DocumentId,
    Guid RegistryId,
    string DocumentTitle,
    string? CentralNumberText,
    string RegistryNumberText,
    int NumberingYear,
    DateTime? RegistryDate,
    int StatusId,
    int RegistrationCount,
    string? ReplacedByNumberText,
    string? ReplacesNumberText,
    Guid? VisitId,
    bool? RecordLocked,
    IReadOnlyDictionary<string, string?> Values);

internal sealed class ListDepartmentRegisterEntriesQueryHandler
    : IQueryHandler<ListDepartmentRegisterEntriesQuery, PagedResult<DepartmentRegisterEntryResponse>>
{
    private const string FieldSortPrefix = "field:";

    private readonly IDepartmentRepository _departmentRepository;
    private readonly IDocumentNumberRepository _documentNumberRepository;
    private readonly IDocumentTypeSchemaVersionRepository _versionRepository;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public ListDepartmentRegisterEntriesQueryHandler(
        IDepartmentRepository departmentRepository,
        IDocumentNumberRepository documentNumberRepository,
        IDocumentTypeSchemaVersionRepository versionRepository,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUserContext userContext,
        IClock clock)
    {
        _departmentRepository = departmentRepository;
        _documentNumberRepository = documentNumberRepository;
        _versionRepository = versionRepository;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _userContext = userContext;
        _clock = clock;
    }

    public async Task<Result<PagedResult<DepartmentRegisterEntryResponse>>> Handle(
        ListDepartmentRegisterEntriesQuery request,
        CancellationToken cancellationToken)
    {
        if (await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken) is null)
            return Result.Failure<PagedResult<DepartmentRegisterEntryResponse>>(DomainErrors.Department.NotFound);

        if (!await _departmentScopeEvaluator.CanAccessDepartmentAsync(request.DepartmentId, cancellationToken))
        {
            return Result.Failure<PagedResult<DepartmentRegisterEntryResponse>>(
                DomainErrors.Correspondence.DepartmentOutOfScope);
        }

        DocumentTypeSchemaVersion? version = null;
        if (request.SchemaVersionId is { } versionId)
        {
            version = await _versionRepository.GetByIdAsync(versionId, cancellationToken);
            if (version is null
                || version.DepartmentId != request.DepartmentId
                || version.Status == DocumentTypeSchemaVersionStatus.Draft)
            {
                return Result.Failure<PagedResult<DepartmentRegisterEntryResponse>>(
                    DomainErrors.DepartmentRecords.FormNotFound);
            }
        }

        // A sort on a record field names the field by key; the repository needs its id and type.
        var sortBy = request.SortBy;
        DocumentFieldSort? fieldSort = null;
        if (sortBy is not null && sortBy.StartsWith(FieldSortPrefix, StringComparison.Ordinal))
        {
            var key = sortBy[FieldSortPrefix.Length..];
            var field = version?.Fields.FirstOrDefault(candidate => candidate.FieldKey == key);
            if (field is not null)
                fieldSort = new DocumentFieldSort(field.DocumentFieldId, field.DataType);
            sortBy = null;
        }

        var page = new PageRequest(request.Page, request.PageSize, request.Search, sortBy, request.SortDescending);
        var access = new DocumentViewAccess(
            _userContext.UserId,
            await _departmentScopeEvaluator.GetAccessibleDepartmentIdsAsync(cancellationToken),
            _clock.UtcNow);
        var filter = new DepartmentRegisterFilter(
            request.DepartmentId,
            request.RegistryId,
            request.NumberingYear,
            request.StatusId,
            request.DateFrom,
            request.DateTo,
            DocumentTypeId: version?.DocumentTypeId,
            SchemaVersionId: version?.SchemaVersionId,
            FieldSort: fieldSort);

        var result = await _documentNumberRepository.ListDepartmentRegisterPagedAsync(
            page, access, filter, cancellationToken);

        var fields = version?.Fields.ToList() ?? [];
        var items = result.Items.Select(row =>
        {
            var stored = row.RecordVisit?.Values.ToDictionary(value => value.DocumentFieldId)
                ?? new Dictionary<Guid, DepartmentRecordValue>();
            var values = fields.ToDictionary(
                field => field.FieldKey,
                field => stored.TryGetValue(field.DocumentFieldId, out var value)
                    ? DocumentFieldValueText.Format(field.DataType, value.AsFieldValue())
                    : null);

            return new DepartmentRegisterEntryResponse(
                row.Number.DocumentNumberId,
                row.Number.DocumentId,
                row.Number.RegistryId,
                row.DocumentTitle ?? string.Empty,
                row.CentralNumberText,
                row.Number.OfficialNumberText,
                row.Number.NumberingYear,
                row.Number.RegistryDate,
                row.Number.StatusId,
                row.RegistrationCount,
                row.ReplacedByNumberText,
                row.ReplacesNumberText,
                row.RecordVisit?.VisitId,
                row.RecordVisit?.IsLocked,
                values);
        }).ToList();

        return Result.Success(new PagedResult<DepartmentRegisterEntryResponse>(
            items, page.NormalizedPage, page.NormalizedPageSize, result.TotalCount));
    }
}
