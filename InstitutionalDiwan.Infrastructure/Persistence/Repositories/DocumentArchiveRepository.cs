using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Archiving;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentArchiveRepository : IDocumentArchiveRepository
{
    private readonly DiwanDbContext _context;

    public DocumentArchiveRepository(DiwanDbContext context) => _context = context;

    public async Task<IReadOnlyList<DocumentArchiveEvent>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentArchiveEvents
            .AsNoTracking()
            .Where(archiveEvent => archiveEvent.DocumentId == documentId)
            .OrderByDescending(archiveEvent => archiveEvent.OccurredAt)
            .ToListAsync(cancellationToken);

    public async Task<PagedList<ArchivedDocumentRow>> SearchAsync(
        ArchiveSearchFilter filter,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var archivedEvents = _context.DocumentArchiveEvents
            .Where(archiveEvent => archiveEvent.EventTypeId == ArchiveEventType.ArchivedId);

        var documents = _context.Documents.AsNoTracking()
            .Where(document => document.CurrentStatusId == DocumentStatusType.ArchivedId)
            .WhereViewableBy(
                _context,
                new DocumentViewAccess(filter.CallerUserId, filter.AccessibleDepartmentIds, filter.AsOfUtc));

        if (filter.OwningDepartmentId is { } departmentId)
            documents = documents.Where(document => document.OwningDepartmentId == departmentId);

        if (filter.DocumentTypeId is { } type)
            documents = documents.Where(document => document.DocumentTypeId == type);

        if (filter.DirectionId is { } direction)
            documents = documents.Where(document => document.DirectionId == direction);

        foreach (var fieldFilter in filter.FieldFilters ?? [])
            documents = documents.WhereFieldMatches(_context, fieldFilter);

        if (page.NormalizedSearch is { } search)
        {
            var pattern = $"%{search}%";
            documents = documents.Where(document =>
                EF.Functions.Like(document.DisplayName, pattern) ||
                (document.OfficialNumberText != null && EF.Functions.Like(document.OfficialNumberText, pattern)));
        }

        // A document archived more than once appears once, with the event that archived it last.
        var query =
            from document in documents
            join archiveEvent in archivedEvents
                on document.DocumentId equals archiveEvent.DocumentId
            where archiveEvent.ArchiveEventId == archivedEvents
                .Where(candidate => candidate.DocumentId == document.DocumentId)
                .OrderByDescending(candidate => candidate.OccurredAt)
                .ThenByDescending(candidate => candidate.ArchiveEventId)
                .Select(candidate => candidate.ArchiveEventId)
                .FirstOrDefault()
            select new { Document = document, Event = archiveEvent };

        if (filter.ArchiveLocationId is { } location)
            query = query.Where(row => row.Event.ArchiveLocationId == location);

        if (filter.ArchivedFromUtc is { } from)
            query = query.Where(row => row.Event.OccurredAt >= from);

        if (filter.ArchivedToUtc is { } to)
            query = query.Where(row => row.Event.OccurredAt <= to);

        query = (page.NormalizedSortBy, page.SortDescending) switch
        {
            ("title", true) => query.OrderByDescending(row => row.Document.DisplayName),
            ("title", false) => query.OrderBy(row => row.Document.DisplayName),
            ("officialnumber", true) => query.OrderByDescending(row => row.Document.OfficialNumberText),
            ("officialnumber", false) => query.OrderBy(row => row.Document.OfficialNumberText),
            (_, true) => query.OrderByDescending(row => row.Event.OccurredAt),
            _ => query.OrderBy(row => row.Event.OccurredAt)
        };

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);

        return new PagedList<ArchivedDocumentRow>(
            rows.Select(row => new ArchivedDocumentRow(
                row.Document,
                row.Event.OccurredAt,
                row.Event.ActorUserId,
                row.Event.ArchiveLocationId)).ToList(),
            totalCount);
    }

    public void Add(DocumentArchiveEvent archiveEvent) => _context.DocumentArchiveEvents.Add(archiveEvent);
}
