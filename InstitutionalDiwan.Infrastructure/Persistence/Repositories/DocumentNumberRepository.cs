using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Numbering;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentNumberRepository : IDocumentNumberRepository
{
    private readonly DiwanDbContext _context;

    public DocumentNumberRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentNumber?> GetByIdAsync(Guid documentNumberId, CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers
            .Include(number => number.History)
            .FirstOrDefaultAsync(number => number.DocumentNumberId == documentNumberId, cancellationToken);

    /// <summary>
    /// The active <em>central</em> number. Department entries are separate lines and must never be
    /// picked up by the cancel/replace lifecycle.
    /// </summary>
    public Task<DocumentNumber?> GetActiveByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers
            .Include(number => number.History)
            .FirstOrDefaultAsync(
                number => number.DocumentId == documentId &&
                          number.ScopeId == DocumentNumberScope.CentralId &&
                          number.StatusId == DocumentNumberStatus.ActiveId,
                cancellationToken);

    public Task<bool> ExistsByNumberAndYearAsync(
        string officialNumberText,
        int numberingYear,
        CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers.AnyAsync(
            number => number.OfficialNumberText == officialNumberText && number.NumberingYear == numberingYear,
            cancellationToken);

    public Task<bool> ExistsInScopeAsync(
        int scopeId,
        Guid registryId,
        string officialNumberText,
        int numberingYear,
        CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers.AnyAsync(
            number => number.ScopeId == scopeId &&
                      number.RegistryId == registryId &&
                      number.OfficialNumberText == officialNumberText &&
                      number.NumberingYear == numberingYear,
            cancellationToken);

    public Task<DocumentNumber?> GetDepartmentEntryAsync(
        Guid documentId,
        Guid departmentId,
        CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers
            .Include(number => number.History)
            .FirstOrDefaultAsync(
                number => number.DocumentId == documentId &&
                          number.ScopeId == DocumentNumberScope.DepartmentId &&
                          number.DepartmentId == departmentId,
                cancellationToken);

    public async Task<Guid?> FindDocumentIdByVisitAsync(Guid visitId, CancellationToken cancellationToken = default) =>
        await (from visit in _context.DocumentRegistryVisits
               join number in _context.DocumentNumbers on visit.DocumentNumberId equals number.DocumentNumberId
               where visit.VisitId == visitId
               select (Guid?)number.DocumentId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<DocumentNumber>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentNumbers
            .Include(number => number.History)
            .Include(number => number.Visits)
                .ThenInclude(visit => visit.Values)
            .AsSplitQuery()
            .Where(number => number.DocumentId == documentId)
            .OrderBy(number => number.IssuedAt)
            .ToListAsync(cancellationToken);

    public async Task<PagedList<DepartmentRegisterRow>> ListDepartmentRegisterPagedAsync(
        PageRequest page,
        DocumentViewAccess access,
        DepartmentRegisterFilter filter,
        CancellationToken cancellationToken = default)
    {
        var lines = _context.DocumentNumbers.AsNoTracking()
            .Where(number => number.ScopeId == DocumentNumberScope.DepartmentId &&
                             number.DepartmentId == filter.DepartmentId);

        if (filter.RegistryId is { } registryId)
            lines = lines.Where(number => number.RegistryId == registryId);
        if (filter.NumberingYear is { } year)
            lines = lines.Where(number => number.NumberingYear == year);
        if (filter.StatusId is { } statusId)
            lines = lines.Where(number => number.StatusId == statusId);
        if (filter.RegistryDateFrom is { } from)
        {
            var start = from.Date;
            lines = lines.Where(number => number.RegistryDate >= start);
        }
        if (filter.RegistryDateTo is { } to)
        {
            var endExclusive = to.Date.AddDays(1);
            lines = lines.Where(number => number.RegistryDate < endExclusive);
        }
        if (filter.DocumentTypeId is { } typeId)
        {
            var ofType = _context.Documents.Where(document => document.DocumentTypeId == typeId)
                .Select(document => document.DocumentId);
            lines = lines.Where(number => ofType.Contains(number.DocumentId));
        }

        var versionId = filter.SchemaVersionId;
        if (versionId is { } version)
            lines = lines.Where(number => number.Visits.Any(visit => visit.SchemaVersionId == version));

        if (versionId is { } filteredVersion && filter.FieldFilters is { Count: > 0 } fieldFilters)
        {
            foreach (var fieldFilter in fieldFilters)
                lines = lines.WhereRecordFieldMatches(_context, filteredVersion, fieldFilter);
        }

        // A line stays in the book even when its document is hidden from the caller, but the hidden
        // document's title and central number are neither shown nor searchable.
        var viewable = _context.Documents.AsNoTracking().WhereViewableBy(_context, access);
        var query =
            from number in lines
            join document in viewable on number.DocumentId equals document.DocumentId into documents
            from document in documents.DefaultIfEmpty()
            select new RegisterLine
            {
                Number = number,
                Title = document == null ? null : document.DisplayName,
                Central = document == null ? null : document.OfficialNumberText,
                Count = number.Visits.Count()
            };

        if (page.NormalizedSearch is { } search)
        {
            var pattern = $"%{search}%";
            if (versionId is { } searched)
            {
                // The form values the page shows are the latest visit's, so only those are searched.
                var latestVisits = DepartmentRecordFieldFilterQuery.LatestVisits(_context, searched);
                query = query.Where(row =>
                    EF.Functions.Like(row.Number.OfficialNumberText, pattern) ||
                    (row.Title != null && EF.Functions.Like(row.Title, pattern)) ||
                    (row.Central != null && EF.Functions.Like(row.Central, pattern)) ||
                    latestVisits.Any(visit =>
                        visit.DocumentNumberId == row.Number.DocumentNumberId &&
                        visit.Values.Any(value =>
                            (value.TextValue != null && EF.Functions.Like(value.TextValue, pattern)) ||
                            (value.OptionKey != null && EF.Functions.Like(value.OptionKey, pattern)))));
            }
            else
            {
                query = query.Where(row =>
                    EF.Functions.Like(row.Number.OfficialNumberText, pattern) ||
                    (row.Title != null && EF.Functions.Like(row.Title, pattern)) ||
                    (row.Central != null && EF.Functions.Like(row.Central, pattern)));
            }
        }

        var ordered = filter.FieldSort is { } sort && versionId is not null
            ? OrderByRecordField(query, sort, versionId.Value, page.SortDescending)
            : OrderByColumn(query, page);

        var total = await ordered.CountAsync(cancellationToken);
        var rows = await ordered.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        if (rows.Count == 0)
            return new PagedList<DepartmentRegisterRow>(Array.Empty<DepartmentRegisterRow>(), total);

        var lineIds = rows.Select(row => row.Number.DocumentNumberId).ToList();

        // The number this line was replaced by, and the number it replaced, may sit outside the page.
        var linked = await _context.DocumentNumbers.AsNoTracking()
            .Where(other => lineIds.Contains(other.DocumentNumberId) ||
                            (other.ReplacedByNumberId != null && lineIds.Contains(other.ReplacedByNumberId.Value)))
            .Select(other => new { other.DocumentNumberId, other.OfficialNumberText, other.ReplacedByNumberId })
            .ToListAsync(cancellationToken);
        var textById = linked.ToDictionary(other => other.DocumentNumberId, other => other.OfficialNumberText);
        var replacesById = linked
            .Where(other => other.ReplacedByNumberId != null)
            .GroupBy(other => other.ReplacedByNumberId!.Value)
            .ToDictionary(group => group.Key, group => group.First().OfficialNumberText);

        var latestVisit = new Dictionary<Guid, DocumentRegistryVisit>();
        if (versionId is { } pinned)
        {
            var visits = await _context.DocumentRegistryVisits.AsNoTracking()
                .Include(visit => visit.Values)
                .Where(visit => lineIds.Contains(visit.DocumentNumberId) && visit.SchemaVersionId == pinned)
                .ToListAsync(cancellationToken);
            latestVisit = visits
                .GroupBy(visit => visit.DocumentNumberId)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(visit => visit.RegistryDate)
                        .ThenByDescending(visit => visit.RecordedAt)
                        .First());
        }

        var items = rows.Select(row => new DepartmentRegisterRow(
            row.Number,
            row.Title,
            row.Central,
            row.Count,
            row.Number.ReplacedByNumberId is { } replacedBy ? textById.GetValueOrDefault(replacedBy) : null,
            replacesById.GetValueOrDefault(row.Number.DocumentNumberId),
            latestVisit.GetValueOrDefault(row.Number.DocumentNumberId))).ToList();

        return new PagedList<DepartmentRegisterRow>(items, total);
    }

    /// <summary>A register line with the document facts the page needs, as one projected row.</summary>
    private sealed class RegisterLine
    {
        public DocumentNumber Number { get; init; } = null!;
        public string? Title { get; init; }
        public string? Central { get; init; }
        public int Count { get; init; }
    }

    private static IOrderedQueryable<RegisterLine> OrderByColumn(IQueryable<RegisterLine> query, PageRequest page)
    {
        var descending = page.SortDescending;
        var ordered = page.NormalizedSortBy switch
        {
            "registrynumber" => descending
                ? query.OrderByDescending(row => row.Number.OfficialNumberText)
                : query.OrderBy(row => row.Number.OfficialNumberText),
            "document" => descending
                ? query.OrderByDescending(row => row.Title)
                : query.OrderBy(row => row.Title),
            "centralnumber" => descending
                ? query.OrderByDescending(row => row.Central)
                : query.OrderBy(row => row.Central),
            "numberingyear" => descending
                ? query.OrderByDescending(row => row.Number.NumberingYear)
                : query.OrderBy(row => row.Number.NumberingYear),
            "status" => descending
                ? query.OrderByDescending(row => row.Number.StatusId)
                : query.OrderBy(row => row.Number.StatusId),
            "registrationcount" => descending
                ? query.OrderByDescending(row => row.Count)
                : query.OrderBy(row => row.Count),
            "registrydate" or null or _ => descending
                ? query.OrderByDescending(row => row.Number.RegistryDate)
                : query.OrderBy(row => row.Number.RegistryDate),
        };

        return ordered.ThenBy(row => row.Number.OfficialNumberText).ThenBy(row => row.Number.DocumentNumberId);
    }

    /// <summary>Orders by the value of one record field on the line's latest visit of the chosen version.</summary>
    private IOrderedQueryable<RegisterLine> OrderByRecordField(
        IQueryable<RegisterLine> query, DocumentFieldSort sort, Guid versionId, bool descending)
    {
        var fieldId = sort.DocumentFieldId;
        var valuesOfField = _context.DepartmentRecordValues.Where(value => value.DocumentFieldId == fieldId);
        var visitsOfVersion = DepartmentRecordFieldFilterQuery.LatestVisits(_context, versionId);

        var ordered = (sort.DataType, descending) switch
        {
            (DocumentFieldDataType.Number, true) => query.OrderByDescending(row => valuesOfField.Where(value => visitsOfVersion.Any(visit => visit.VisitId == value.VisitId && visit.DocumentNumberId == row.Number.DocumentNumberId)).Select(value => value.NumberValue).FirstOrDefault()),
            (DocumentFieldDataType.Number, false) => query.OrderBy(row => valuesOfField.Where(value => visitsOfVersion.Any(visit => visit.VisitId == value.VisitId && visit.DocumentNumberId == row.Number.DocumentNumberId)).Select(value => value.NumberValue).FirstOrDefault()),
            (DocumentFieldDataType.Date, true) => query.OrderByDescending(row => valuesOfField.Where(value => visitsOfVersion.Any(visit => visit.VisitId == value.VisitId && visit.DocumentNumberId == row.Number.DocumentNumberId)).Select(value => value.DateValue).FirstOrDefault()),
            (DocumentFieldDataType.Date, false) => query.OrderBy(row => valuesOfField.Where(value => visitsOfVersion.Any(visit => visit.VisitId == value.VisitId && visit.DocumentNumberId == row.Number.DocumentNumberId)).Select(value => value.DateValue).FirstOrDefault()),
            (DocumentFieldDataType.Boolean, true) => query.OrderByDescending(row => valuesOfField.Where(value => visitsOfVersion.Any(visit => visit.VisitId == value.VisitId && visit.DocumentNumberId == row.Number.DocumentNumberId)).Select(value => value.BoolValue).FirstOrDefault()),
            (DocumentFieldDataType.Boolean, false) => query.OrderBy(row => valuesOfField.Where(value => visitsOfVersion.Any(visit => visit.VisitId == value.VisitId && visit.DocumentNumberId == row.Number.DocumentNumberId)).Select(value => value.BoolValue).FirstOrDefault()),
            (DocumentFieldDataType.Select, true) => query.OrderByDescending(row => valuesOfField.Where(value => visitsOfVersion.Any(visit => visit.VisitId == value.VisitId && visit.DocumentNumberId == row.Number.DocumentNumberId)).Select(value => value.OptionKey).FirstOrDefault()),
            (DocumentFieldDataType.Select, false) => query.OrderBy(row => valuesOfField.Where(value => visitsOfVersion.Any(visit => visit.VisitId == value.VisitId && visit.DocumentNumberId == row.Number.DocumentNumberId)).Select(value => value.OptionKey).FirstOrDefault()),
            (_, true) => query.OrderByDescending(row => valuesOfField.Where(value => visitsOfVersion.Any(visit => visit.VisitId == value.VisitId && visit.DocumentNumberId == row.Number.DocumentNumberId)).Select(value => value.TextValue).FirstOrDefault()),
            _ => query.OrderBy(row => valuesOfField.Where(value => visitsOfVersion.Any(visit => visit.VisitId == value.VisitId && visit.DocumentNumberId == row.Number.DocumentNumberId)).Select(value => value.TextValue).FirstOrDefault())
        };

        return ordered.ThenBy(row => row.Number.DocumentNumberId);
    }

    public async Task<IReadOnlyList<DocumentNumber>> ListByDepartmentAsync(
        Guid departmentId,
        int? numberingYear,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentNumbers
            .Include(number => number.History)
            .Where(number => number.DepartmentId == departmentId &&
                             (numberingYear == null || number.NumberingYear == numberingYear))
            .OrderBy(number => number.RegistryDate)
            .ThenBy(number => number.OfficialNumberText)
            .ToListAsync(cancellationToken);

    public Task<bool> AnyByRegistryAsync(Guid registryId, CancellationToken cancellationToken = default) =>
        _context.DocumentNumbers.AnyAsync(
            number => number.RegistryId == registryId &&
                      number.ScopeId == DocumentNumberScope.CentralId,
            cancellationToken);

    public void Add(DocumentNumber documentNumber) => _context.DocumentNumbers.Add(documentNumber);
}
