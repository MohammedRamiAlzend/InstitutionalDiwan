using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Decisions;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentDecisionRepository : IDocumentDecisionRepository
{
    private readonly DiwanDbContext _context;

    public DocumentDecisionRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentDecision?> GetByIdAsync(Guid decisionId, CancellationToken cancellationToken = default) =>
        _context.DocumentDecisions
            .Include(decision => decision.Appeals)
            .FirstOrDefaultAsync(decision => decision.DecisionId == decisionId, cancellationToken);

    public Task<DocumentDecision?> GetActiveByDocumentAsync(Guid documentId, CancellationToken cancellationToken = default) =>
        _context.DocumentDecisions.FirstOrDefaultAsync(
            decision => decision.DocumentId == documentId &&
                        (decision.StatusId == DecisionStatus.IssuedId ||
                         decision.StatusId == DecisionStatus.AppealedId ||
                         decision.StatusId == DecisionStatus.FinalId),
            cancellationToken);

    public async Task<IReadOnlyList<DocumentDecision>> ListByDocumentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentDecisions.AsNoTracking()
            .Include(decision => decision.Appeals)
            .Where(decision => decision.DocumentId == documentId)
            .OrderByDescending(decision => decision.RecordedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> IsNumberTakenAsync(
        Guid issuingDepartmentId,
        int year,
        string decisionNumber,
        CancellationToken cancellationToken = default) =>
        _context.DocumentDecisions.AnyAsync(
            decision => decision.IssuingDepartmentId == issuingDepartmentId &&
                        decision.DecisionYear == year &&
                        decision.DecisionNumber == decisionNumber,
            cancellationToken);

    public async Task<OpenDecisionVisit?> FindOpenVisitAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var row = await (
            from visit in _context.DocumentRegistryVisits.AsNoTracking()
            join number in _context.DocumentNumbers.AsNoTracking()
                on visit.DocumentNumberId equals number.DocumentNumberId
            where number.DocumentId == documentId &&
                  visit.LockedAt == null &&
                  number.DepartmentId != null
            orderby visit.RecordedAt descending
            select new { visit.VisitId, DepartmentId = number.DepartmentId!.Value }
        ).FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : new OpenDecisionVisit(row.VisitId, row.DepartmentId);
    }

    public async Task<PagedList<DecisionRegisterRow>> ListPagedAsync(
        PageRequest page,
        DocumentViewAccess access,
        DecisionRegisterFilter filter,
        CancellationToken cancellationToken = default)
    {
        var viewable = _context.Documents.AsNoTracking().WhereViewableBy(_context, access);
        var query =
            from decision in _context.DocumentDecisions.AsNoTracking()
            join document in viewable on decision.DocumentId equals document.DocumentId
            join type in _context.DecisionTypes.AsNoTracking() on decision.DecisionTypeId equals type.DecisionTypeId
            select new
            {
                decision,
                TypeNameAr = type.NameAr,
                Title = document.DisplayName,
                document.OfficialNumberText,
            };

        if (filter.DecisionTypeId is { } typeId)
            query = query.Where(row => row.decision.DecisionTypeId == typeId);
        if (filter.StatusId is { } statusId)
            query = query.Where(row => row.decision.StatusId == statusId);
        if (filter.IssuingDepartmentId is { } departmentId)
            query = query.Where(row => row.decision.IssuingDepartmentId == departmentId);
        if (filter.Year is { } year)
            query = query.Where(row => row.decision.DecisionYear == year);

        if (page.NormalizedSearch is { } search)
        {
            query = query.Where(row =>
                row.decision.DecisionNumber.Contains(search) ||
                row.Title.Contains(search) ||
                (row.OfficialNumberText != null && row.OfficialNumberText.Contains(search)));
        }

        query = page.NormalizedSortBy switch
        {
            "decisionnumber" => page.SortDescending
                ? query.OrderByDescending(row => row.decision.DecisionNumber)
                : query.OrderBy(row => row.decision.DecisionNumber),
            "decisiondate" => page.SortDescending
                ? query.OrderByDescending(row => row.decision.DecisionDate)
                : query.OrderBy(row => row.decision.DecisionDate),
            "status" => page.SortDescending
                ? query.OrderByDescending(row => row.decision.StatusId)
                : query.OrderBy(row => row.decision.StatusId),
            "type" => page.SortDescending
                ? query.OrderByDescending(row => row.TypeNameAr)
                : query.OrderBy(row => row.TypeNameAr),
            _ => query.OrderByDescending(row => row.decision.RecordedAt),
        };

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip(page.Skip).Take(page.Take).ToListAsync(cancellationToken);
        return new PagedList<DecisionRegisterRow>(
            items.Select(row => new DecisionRegisterRow(row.decision, row.TypeNameAr, row.Title, row.OfficialNumberText)).ToList(),
            total);
    }

    public void Add(DocumentDecision decision) => _context.DocumentDecisions.Add(decision);
}
