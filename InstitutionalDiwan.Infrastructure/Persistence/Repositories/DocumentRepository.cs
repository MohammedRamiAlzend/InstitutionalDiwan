using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
            .FirstOrDefaultAsync(document => document.DocumentId == documentId, cancellationToken);

    public async Task<PagedList<Document>> ListPagedAsync(
        PageRequest page,
        DocumentViewAccess access,
        int? statusId = null,
        Guid? owningDepartmentId = null,
        int? directionId = null,
        int? documentTypeId = null,
        IReadOnlyList<DocumentFieldFilter>? fieldFilters = null,
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
            query = ApplyFieldFilter(query, filter);

        if (page.NormalizedSearch is { } search)
        {
            var pattern = $"%{search}%";
            query = query.Where(document =>
                EF.Functions.Like(document.Title, pattern) ||
                (document.SubjectSummary != null && EF.Functions.Like(document.SubjectSummary, pattern)) ||
                (document.OfficialNumberText != null && EF.Functions.Like(document.OfficialNumberText, pattern)));
        }

        query = (page.NormalizedSortBy, page.SortDescending) switch
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

    /// <summary>
    /// Keeps documents that store a value for the filtered field satisfying the comparison. Dates are
    /// compared by calendar day: <c>eq</c> matches the whole day, <c>gt</c> starts after it and
    /// <c>lte</c> ends with it.
    /// </summary>
    private IQueryable<Document> ApplyFieldFilter(IQueryable<Document> query, DocumentFieldFilter filter)
    {
        var fieldId = filter.DocumentFieldId;
        Expression<Func<DocumentFieldValue, bool>> predicate;

        switch (filter.DataType)
        {
            case DocumentFieldDataType.Text:
            case DocumentFieldDataType.LongText:
            {
                var text = filter.Text!;
                if (filter.Operator == FieldFilterOperator.Contains)
                {
                    var pattern = $"%{EscapeLike(text)}%";
                    predicate = v => v.TextValue != null && EF.Functions.Like(v.TextValue, pattern, "\\");
                }
                else
                {
                    predicate = v => v.TextValue == text;
                }
                break;
            }

            case DocumentFieldDataType.Number:
            {
                var number = filter.Number!.Value;
                predicate = filter.Operator switch
                {
                    FieldFilterOperator.GreaterThan => v => v.NumberValue > number,
                    FieldFilterOperator.GreaterThanOrEqual => v => v.NumberValue >= number,
                    FieldFilterOperator.LessThan => v => v.NumberValue < number,
                    FieldFilterOperator.LessThanOrEqual => v => v.NumberValue <= number,
                    _ => v => v.NumberValue == number
                };
                break;
            }

            case DocumentFieldDataType.Date:
            {
                var dayStart = filter.Date!.Value.Date;
                var dayEnd = dayStart.AddDays(1);
                predicate = filter.Operator switch
                {
                    FieldFilterOperator.GreaterThan => v => v.DateValue >= dayEnd,
                    FieldFilterOperator.GreaterThanOrEqual => v => v.DateValue >= dayStart,
                    FieldFilterOperator.LessThan => v => v.DateValue < dayStart,
                    FieldFilterOperator.LessThanOrEqual => v => v.DateValue < dayEnd,
                    _ => v => v.DateValue >= dayStart && v.DateValue < dayEnd
                };
                break;
            }

            case DocumentFieldDataType.Boolean:
            {
                var boolean = filter.Boolean!.Value;
                predicate = v => v.BoolValue == boolean;
                break;
            }

            default:
            {
                var optionKey = filter.OptionKey!;
                predicate = v => v.OptionKey == optionKey;
                break;
            }
        }

        return query.Where(document => _context.DocumentFieldValues
            .Where(value => value.DocumentFieldId == fieldId)
            .Where(predicate)
            .Any(value => value.DocumentId == document.DocumentId));
    }

    /// <summary>Escapes the LIKE wildcards so a filter for "100%" matches the literal text.</summary>
    private static string EscapeLike(string term) =>
        term.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
