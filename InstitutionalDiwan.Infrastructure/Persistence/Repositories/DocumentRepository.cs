using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentRepository : IDocumentRepository
{
    private readonly DiwanDbContext _context;

    public DocumentRepository(DiwanDbContext context) => _context = context;

    public Task<Document?> GetByIdAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        _context.Documents
            .Include(document => document.Participants)
            .Include(document => document.StatusHistory)
            .Include(document => document.FieldValues)
            .AsSplitQuery()
            .FirstOrDefaultAsync(document => document.DocumentId == documentId, cancellationToken);

    public Task<Document?> GetWithNumbersAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        _context.Documents
            .Include(document => document.Numbers)
                .ThenInclude(number => number.History)
            .Include(document => document.Numbers)
                .ThenInclude(number => number.Visits)
            .AsSplitQuery()
            .FirstOrDefaultAsync(document => document.DocumentId == documentId, cancellationToken);

    public async Task<PagedList<Document>> ListPagedAsync(
        PageRequest page,
        DocumentViewAccess access,
        int? statusId = null,
        Guid? owningDepartmentId = null,
        int? directionId = null,
        int? documentTypeId = null,
        IReadOnlyList<DocumentFieldFilter>? fieldFilters = null,
        DocumentFieldSort? fieldSort = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Documents.AsNoTracking().WhereViewableBy(_context, access);

        if (statusId is { } status)
            query = query.Where(document => document.CurrentStatusId == status);

        if (owningDepartmentId is { } departmentId)
            query = query.Where(document => document.OwningDepartmentId == departmentId);

        if (directionId is { } direction)
            query = query.Where(document => document.DirectionId == direction);

        if (documentTypeId is { } type)
            query = query.Where(document => document.DocumentTypeId == type);

        // Filters run after the visibility predicate, so they can only narrow what the caller may see.
        foreach (var filter in fieldFilters ?? [])
            query = query.WhereFieldMatches(_context, filter);

        if (page.NormalizedSearch is { } search)
        {
            var pattern = $"%{search}%";
            query = query.Where(document =>
                EF.Functions.Like(document.Title, pattern) ||
                (document.SubjectSummary != null && EF.Functions.Like(document.SubjectSummary, pattern)) ||
                (document.OfficialNumberText != null && EF.Functions.Like(document.OfficialNumberText, pattern)));
        }

        query = fieldSort is not null ? OrderByField(query, fieldSort, page.SortDescending) : (page.NormalizedSortBy, page.SortDescending) switch
        {
            ("title", true) => query.OrderByDescending(document => document.Title),
            ("title", false) => query.OrderBy(document => document.Title),
            ("registrationdate", true) => query.OrderByDescending(document => document.RegistrationDate),
            ("registrationdate", false) => query.OrderBy(document => document.RegistrationDate),
            ("officialnumber", true) => query.OrderByDescending(document => document.OfficialNumberText),
            ("officialnumber", false) => query.OrderBy(document => document.OfficialNumberText),
            (_, true) => query.OrderByDescending(document => document.CreatedAt),
            _ => query.OrderBy(document => document.CreatedAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);

        return new PagedList<Document>(items, totalCount);
    }

    public async Task<IReadOnlyList<Document>> GetByIdsAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken = default)
    {
        if (documentIds.Count == 0)
            return Array.Empty<Document>();

        return await _context.Documents
            .Where(document => documentIds.Contains(document.DocumentId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentFieldValue>> ListFieldValuesAsync(
        IReadOnlyCollection<Guid> documentIds,
        IReadOnlyCollection<Guid> documentFieldIds,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentFieldValues
            .AsNoTracking()
            .Where(value => documentIds.Contains(value.DocumentId) && documentFieldIds.Contains(value.DocumentFieldId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<Guid>> ListViewableIdsAsync(
        IReadOnlyCollection<Guid> documentIds,
        DocumentViewAccess access,
        CancellationToken cancellationToken = default)
    {
        if (documentIds.Count == 0)
            return new HashSet<Guid>();

        var viewable = await _context.Documents
            .Where(document => documentIds.Contains(document.DocumentId))
            .WhereViewableBy(_context, access)
            .Select(document => document.DocumentId)
            .ToListAsync(cancellationToken);

        return viewable.ToHashSet();
    }

    public void Add(Document document) => _context.Documents.Add(document);

    /// <summary>Orders by the value of one dynamic field; documents without a value keep their place together.</summary>
    private IQueryable<Document> OrderByField(IQueryable<Document> query, DocumentFieldSort sort, bool descending)
    {
        var values = _context.DocumentFieldValues.Where(value => value.DocumentFieldId == sort.DocumentFieldId);

        var ordered = (sort.DataType, descending) switch
        {
            (DocumentFieldDataType.Number, true) => query.OrderByDescending(d => values.Where(v => v.DocumentId == d.DocumentId).Select(v => v.NumberValue).FirstOrDefault()),
            (DocumentFieldDataType.Number, false) => query.OrderBy(d => values.Where(v => v.DocumentId == d.DocumentId).Select(v => v.NumberValue).FirstOrDefault()),
            (DocumentFieldDataType.Date, true) => query.OrderByDescending(d => values.Where(v => v.DocumentId == d.DocumentId).Select(v => v.DateValue).FirstOrDefault()),
            (DocumentFieldDataType.Date, false) => query.OrderBy(d => values.Where(v => v.DocumentId == d.DocumentId).Select(v => v.DateValue).FirstOrDefault()),
            (DocumentFieldDataType.Boolean, true) => query.OrderByDescending(d => values.Where(v => v.DocumentId == d.DocumentId).Select(v => v.BoolValue).FirstOrDefault()),
            (DocumentFieldDataType.Boolean, false) => query.OrderBy(d => values.Where(v => v.DocumentId == d.DocumentId).Select(v => v.BoolValue).FirstOrDefault()),
            (_, true) => query.OrderByDescending(d => values.Where(v => v.DocumentId == d.DocumentId).Select(v => v.TextValue).FirstOrDefault()),
            _ => query.OrderBy(d => values.Where(v => v.DocumentId == d.DocumentId).Select(v => v.TextValue).FirstOrDefault())
        };

        return ordered.ThenByDescending(document => document.CreatedAt);
    }
}
