using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Relations;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Infrastructure.Persistence.Repositories;

internal sealed class DocumentRelationRepository : IDocumentRelationRepository
{
    // A cycle walk longer than this means a chain no institution would build; stop instead of looping.
    private const int MaxCycleWalkDepth = 50;
    private const int LockTimeoutMilliseconds = 10_000;

    private readonly DiwanDbContext _context;

    public DocumentRelationRepository(DiwanDbContext context) => _context = context;

    public Task<DocumentRelation?> GetByIdAsync(
        Guid relationId,
        CancellationToken cancellationToken = default) =>
        _context.DocumentRelations.FirstOrDefaultAsync(
            relation => relation.RelationId == relationId, cancellationToken);

    public async Task<IReadOnlyList<DocumentRelation>> GetByDocumentIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default) =>
        await _context.DocumentRelations
            .AsNoTracking()
            .Where(relation => relation.SourceDocumentId == documentId || relation.TargetDocumentId == documentId)
            .OrderByDescending(relation => relation.CreatedAt)
            .ThenBy(relation => relation.RelationId)
            .ToListAsync(cancellationToken);

    public Task<DocumentRelationType?> GetTypeByIdAsync(
        int relationTypeId,
        CancellationToken cancellationToken = default) =>
        _context.DocumentRelationTypes.FirstOrDefaultAsync(
            type => type.Id == relationTypeId, cancellationToken);

    public async Task<IReadOnlyList<DocumentRelationType>> ListTypesAsync(
        IReadOnlyCollection<int> relationTypeIds,
        CancellationToken cancellationToken = default)
    {
        var ids = relationTypeIds.ToList();
        return await _context.DocumentRelationTypes
            .AsNoTracking()
            .Where(type => ids.Contains(type.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(
        Guid sourceDocumentId,
        Guid targetDocumentId,
        int relationTypeId,
        CancellationToken cancellationToken = default) =>
        _context.DocumentRelations.AnyAsync(
            relation => relation.SourceDocumentId == sourceDocumentId &&
                        relation.TargetDocumentId == targetDocumentId &&
                        relation.RelationTypeId == relationTypeId,
            cancellationToken);

    public async Task<bool> WouldCreateCycleAsync(
        int relationTypeId,
        Guid sourceDocumentId,
        Guid targetDocumentId,
        CancellationToken cancellationToken = default)
    {
        await LockTypeAsync(relationTypeId, cancellationToken);

        // The new edge is source → target, so it closes a loop when the target already reaches the
        // source by following existing edges of the same type.
        var visited = new HashSet<Guid> { targetDocumentId };
        var frontier = new List<Guid> { targetDocumentId };

        for (var depth = 0; depth < MaxCycleWalkDepth && frontier.Count > 0; depth++)
        {
            var current = frontier;
            var next = await _context.DocumentRelations
                .AsNoTracking()
                .Where(relation => relation.RelationTypeId == relationTypeId &&
                                   current.Contains(relation.SourceDocumentId))
                .Select(relation => relation.TargetDocumentId)
                .Distinct()
                .ToListAsync(cancellationToken);

            if (next.Contains(sourceDocumentId))
                return true;

            frontier = next.Where(visited.Add).ToList();
        }

        return false;
    }

    public async Task<IReadOnlyList<DocumentRelation>> ListTouchingAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken = default)
    {
        if (documentIds.Count == 0)
            return Array.Empty<DocumentRelation>();

        var ids = documentIds.ToList();
        return await _context.DocumentRelations
            .AsNoTracking()
            .Where(relation => ids.Contains(relation.SourceDocumentId) || ids.Contains(relation.TargetDocumentId))
            .OrderBy(relation => relation.CreatedAt)
            .ThenBy(relation => relation.RelationId)
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedList<DocumentRelationEvent>> ListEventsByDocumentAsync(
        Guid documentId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var query = _context.DocumentRelationEvents
            .AsNoTracking()
            .Where(relationEvent =>
                relationEvent.SourceDocumentId == documentId || relationEvent.TargetDocumentId == documentId);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(relationEvent => relationEvent.OccurredAt)
            .ThenByDescending(relationEvent => relationEvent.RelationEventId)
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);

        return new PagedList<DocumentRelationEvent>(items, totalCount);
    }

    public void Add(DocumentRelation relation) => _context.DocumentRelations.Add(relation);

    public void Add(DocumentRelationEvent relationEvent) => _context.DocumentRelationEvents.Add(relationEvent);

    public void Remove(DocumentRelation relation) => _context.DocumentRelations.Remove(relation);

    /// <summary>
    /// Takes a transaction-scoped lock per relation type so two concurrent link requests cannot both
    /// pass the cycle check. Outside SQL Server, or outside a transaction, there is nothing to lock.
    /// </summary>
    private async Task LockTypeAsync(int relationTypeId, CancellationToken cancellationToken)
    {
        if (!_context.Database.IsSqlServer() || _context.Database.CurrentTransaction is null)
            return;

        var resource = $"relations-cycle:{relationTypeId}";
        await _context.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DECLARE @result int;
            EXEC @result = sp_getapplock
                @Resource = {resource},
                @LockMode = 'Exclusive',
                @LockOwner = 'Transaction',
                @LockTimeout = {LockTimeoutMilliseconds};
            IF @result < 0 THROW 50000, 'Could not lock the relation type.', 1;
            """,
            cancellationToken);
    }
}
