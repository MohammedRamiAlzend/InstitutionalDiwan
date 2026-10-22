using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
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
        int? statusId = null,
        Guid? owningDepartmentId = null,
        int? directionId = null,
        int? documentTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Documents.AsNoTracking();

        if (statusId is { } status)
            query = query.Where(document => document.CurrentStatusId == status);

        if (owningDepartmentId is { } departmentId)
            query = query.Where(document => document.OwningDepartmentId == departmentId);

        if (directionId is { } direction)
            query = query.Where(document => document.DirectionId == direction);

        if (documentTypeId is { } type)
            query = query.Where(document => document.DocumentTypeId == type);

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

    public void Add(Document document) => _context.Documents.Add(document);
}
