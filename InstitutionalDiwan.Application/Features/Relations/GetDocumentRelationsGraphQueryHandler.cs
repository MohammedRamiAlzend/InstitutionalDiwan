using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Relations;

internal sealed class GetDocumentRelationsGraphQueryHandler
    : IQueryHandler<GetDocumentRelationsGraphQuery, RelationGraphResponse>
{
    public const int MaxDepth = 3;
    public const int MaxNodes = 100;

    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentRelationRepository _relationRepository;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IDepartmentScopeEvaluator _departmentScopeEvaluator;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public GetDocumentRelationsGraphQueryHandler(
        IDocumentRepository documentRepository,
        IDocumentRelationRepository relationRepository,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IDepartmentScopeEvaluator departmentScopeEvaluator,
        IUserContext userContext,
        IClock clock)
    {
        _documentRepository = documentRepository;
        _relationRepository = relationRepository;
        _documentAccessEvaluator = documentAccessEvaluator;
        _departmentScopeEvaluator = departmentScopeEvaluator;
        _userContext = userContext;
        _clock = clock;
    }

    public async Task<Result<RelationGraphResponse>> Handle(
        GetDocumentRelationsGraphQuery request,
        CancellationToken cancellationToken)
    {
        if (request.Depth is < 1 or > MaxDepth)
            return Result.Failure<RelationGraphResponse>(DomainErrors.Relations.GraphDepthInvalid);

        var root = await _documentRepository.GetByIdAsync(request.DocumentId, cancellationToken);
        if (root is null)
            return Result.Failure<RelationGraphResponse>(DomainErrors.Correspondence.DocumentNotFound);

        if (!await _documentAccessEvaluator.CanAccessDocumentAsync(
                request.DocumentId, AccessLevel.ViewId, cancellationToken))
        {
            return Result.Failure<RelationGraphResponse>(DomainErrors.Correspondence.DocumentAccessDenied);
        }

        var access = new DocumentViewAccess(
            _userContext.UserId,
            await _departmentScopeEvaluator.GetAccessibleDepartmentIdsAsync(cancellationToken),
            _clock.UtcNow);

        var nodes = new Dictionary<Guid, RelationGraphNode>
        {
            [root.DocumentId] = new(RelatedDocumentSummary.From(root), 0)
        };
        var edges = new Dictionary<Guid, RelationGraphEdge>();
        var hiddenRelations = new HashSet<Guid>();
        var hiddenDocuments = new HashSet<Guid>();
        var typeIds = new HashSet<int>();
        var truncated = false;
        var frontier = new List<Guid> { root.DocumentId };

        for (var depth = 1; depth <= request.Depth && frontier.Count > 0 && !truncated; depth++)
        {
            var relations = await _relationRepository.ListTouchingAsync(frontier, cancellationToken);

            // Only documents not yet judged are checked, and a document found hidden is never followed.
            var unknown = relations
                .SelectMany(relation => new[] { relation.SourceDocumentId, relation.TargetDocumentId })
                .Where(id => !nodes.ContainsKey(id) && !hiddenDocuments.Contains(id))
                .Distinct()
                .ToList();

            var viewable = unknown.Count == 0
                ? new HashSet<Guid>()
                : (await _documentRepository.ListViewableIdsAsync(unknown, access, cancellationToken)).ToHashSet();
            foreach (var id in unknown.Where(id => !viewable.Contains(id)))
                hiddenDocuments.Add(id);

            var discovered = new List<Guid>();
            var depthNodes = nodes;
            bool TryInclude(Guid id)
            {
                if (depthNodes.ContainsKey(id) || discovered.Contains(id))
                    return true;

                if (depthNodes.Count + discovered.Count >= MaxNodes)
                    return false;

                discovered.Add(id);
                return true;
            }

            foreach (var relation in relations)
            {
                if (hiddenDocuments.Contains(relation.SourceDocumentId) ||
                    hiddenDocuments.Contains(relation.TargetDocumentId))
                {
                    hiddenRelations.Add(relation.RelationId);
                    continue;
                }

                if (!TryInclude(relation.SourceDocumentId) || !TryInclude(relation.TargetDocumentId))
                {
                    truncated = true;
                    continue;
                }

                edges[relation.RelationId] = new RelationGraphEdge(
                    relation.RelationId,
                    relation.SourceDocumentId,
                    relation.TargetDocumentId,
                    relation.RelationTypeId);
                typeIds.Add(relation.RelationTypeId);
            }

            if (discovered.Count > 0)
            {
                foreach (var document in await _documentRepository.GetByIdsAsync(discovered, cancellationToken))
                    nodes[document.DocumentId] = new RelationGraphNode(RelatedDocumentSummary.From(document), depth);
            }

            frontier = discovered;
        }

        var types = (await _relationRepository.ListTypesAsync(typeIds.ToList(), cancellationToken))
            .Select(RelationTypeResponse.From)
            .OrderBy(type => type.RelationTypeId)
            .ToList();

        return new RelationGraphResponse(
            root.DocumentId,
            nodes.Values.OrderBy(node => node.Depth).ThenBy(node => node.Document.Title).ToList(),
            edges.Values.ToList(),
            types,
            hiddenRelations.Count,
            truncated);
    }
}
