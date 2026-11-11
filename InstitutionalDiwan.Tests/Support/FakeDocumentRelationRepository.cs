using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Relations;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>In-memory relations. The cycle walk matches <c>DocumentRelationRepository.WouldCreateCycleAsync</c>.</summary>
internal sealed class FakeDocumentRelationRepository : IDocumentRelationRepository
{
    private const int MaxCycleWalkDepth = 50;

    public List<DocumentRelation> Relations { get; } = new();
    public List<DocumentRelationEvent> Events { get; } = new();
    public List<DocumentRelationType> Types { get; } = new();

    public Task<DocumentRelation?> GetByIdAsync(Guid relationId, CancellationToken cancellationToken = default)
        => Task.FromResult(Relations.FirstOrDefault(relation => relation.RelationId == relationId));

    public Task<IReadOnlyList<DocumentRelation>> GetByDocumentIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DocumentRelation>>(Relations
            .Where(relation => relation.SourceDocumentId == documentId || relation.TargetDocumentId == documentId)
            .OrderByDescending(relation => relation.CreatedAt)
            .ThenBy(relation => relation.RelationId)
            .ToList());

    public Task<DocumentRelationType?> GetTypeByIdAsync(int relationTypeId, CancellationToken cancellationToken = default)
        => Task.FromResult(Types.FirstOrDefault(type => type.Id == relationTypeId));

    public Task<IReadOnlyList<DocumentRelationType>> ListTypesAsync(
        IReadOnlyCollection<int> relationTypeIds,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DocumentRelationType>>(
            Types.Where(type => relationTypeIds.Contains(type.Id)).ToList());

    public Task<bool> ExistsAsync(
        Guid sourceDocumentId,
        Guid targetDocumentId,
        int relationTypeId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Relations.Any(relation =>
            relation.SourceDocumentId == sourceDocumentId &&
            relation.TargetDocumentId == targetDocumentId &&
            relation.RelationTypeId == relationTypeId));

    public Task<bool> WouldCreateCycleAsync(
        int relationTypeId,
        Guid sourceDocumentId,
        Guid targetDocumentId,
        CancellationToken cancellationToken = default)
    {
        var visited = new HashSet<Guid> { targetDocumentId };
        var frontier = new List<Guid> { targetDocumentId };

        for (var depth = 0; depth < MaxCycleWalkDepth && frontier.Count > 0; depth++)
        {
            var current = frontier;
            var next = Relations
                .Where(relation => relation.RelationTypeId == relationTypeId &&
                                   current.Contains(relation.SourceDocumentId))
                .Select(relation => relation.TargetDocumentId)
                .Distinct()
                .ToList();

            if (next.Contains(sourceDocumentId))
                return Task.FromResult(true);

            frontier = next.Where(visited.Add).ToList();
        }

        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<DocumentRelation>> ListOutgoingAsync(
        Guid sourceDocumentId,
        int relationTypeId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DocumentRelation>>(Relations
            .Where(relation => relation.SourceDocumentId == sourceDocumentId &&
                               relation.RelationTypeId == relationTypeId)
            .OrderByDescending(relation => relation.CreatedAt)
            .ThenByDescending(relation => relation.RelationId)
            .ToList());

    public Task<IReadOnlyList<DocumentRelation>> ListTouchingAsync(
        IReadOnlyCollection<Guid> documentIds,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<DocumentRelation>>(Relations
            .Where(relation => documentIds.Contains(relation.SourceDocumentId) ||
                               documentIds.Contains(relation.TargetDocumentId))
            .ToList());

    public Task<PagedList<DocumentRelationEvent>> ListEventsByDocumentAsync(
        Guid documentId,
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        var items = Events
            .Where(relationEvent =>
                relationEvent.SourceDocumentId == documentId || relationEvent.TargetDocumentId == documentId)
            .OrderByDescending(relationEvent => relationEvent.OccurredAt)
            .Skip(page.Skip)
            .Take(page.Take)
            .ToList();
        return Task.FromResult(new PagedList<DocumentRelationEvent>(items, items.Count));
    }

    public void Add(DocumentRelation relation) => Relations.Add(relation);

    public void Add(DocumentRelationEvent relationEvent) => Events.Add(relationEvent);

    public void Remove(DocumentRelation relation) => Relations.Remove(relation);
}
