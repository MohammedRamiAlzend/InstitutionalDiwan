using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Relations;

namespace InstitutionalDiwan.Application.Features.Relations;

/// <summary>How a relation reads from the document that is being viewed.</summary>
public static class RelationDirection
{
    public const string Outgoing = "Outgoing";
    public const string Incoming = "Incoming";
    public const string Symmetric = "Symmetric";

    public static string For(DocumentRelation relation, DocumentRelationType type, Guid viewedDocumentId) =>
        type.IsSymmetric
            ? Symmetric
            : relation.SourceDocumentId == viewedDocumentId ? Outgoing : Incoming;
}

/// <summary>A relation type with the flags the client needs to draw and explain a link.</summary>
public sealed record RelationTypeResponse(
    int RelationTypeId,
    string Desc,
    string DescAr,
    bool IsSymmetric,
    bool PreventCycles)
{
    public static RelationTypeResponse From(DocumentRelationType type) => new(
        type.Id, type.Desc, type.DescAr, type.IsSymmetric, type.PreventCycles);
}

/// <summary>The few document fields shown next to a link, never the document's content.</summary>
public sealed record RelatedDocumentSummary(
    Guid DocumentId,
    string Title,
    string? OfficialNumberText,
    int StatusId,
    bool IsArchived)
{
    public static RelatedDocumentSummary From(Document document) => new(
        document.DocumentId,
        document.Title,
        document.OfficialNumberText,
        document.CurrentStatusId,
        document.CurrentStatusId == DocumentStatusType.ArchivedId);
}

/// <summary>One link of a document, read from the document it was requested for.</summary>
public sealed record DocumentRelationResponse(
    Guid RelationId,
    Guid SourceDocumentId,
    Guid TargetDocumentId,
    string Direction,
    RelationTypeResponse Type,
    RelatedDocumentSummary RelatedDocument,
    string? Notes,
    Guid CreatedByUserId,
    DateTime CreatedAt)
{
    public static DocumentRelationResponse From(
        DocumentRelation relation,
        DocumentRelationType type,
        Document relatedDocument,
        Guid viewedDocumentId) => new(
        relation.RelationId,
        relation.SourceDocumentId,
        relation.TargetDocumentId,
        RelationDirection.For(relation, type, viewedDocumentId),
        RelationTypeResponse.From(type),
        RelatedDocumentSummary.From(relatedDocument),
        relation.Notes,
        relation.CreatedByUserId,
        relation.CreatedAt);
}

/// <summary>
/// A document's links. <paramref name="HiddenRelationCount"/> counts links to documents the caller may
/// not view; they are omitted from <paramref name="Items"/>.
/// </summary>
public sealed record DocumentRelationsResponse(
    IReadOnlyList<DocumentRelationResponse> Items,
    int HiddenRelationCount);

/// <summary>A document in the relations graph, with its distance from the document the graph is rooted at.</summary>
public sealed record RelationGraphNode(RelatedDocumentSummary Document, int Depth);

public sealed record RelationGraphEdge(
    Guid RelationId,
    Guid SourceDocumentId,
    Guid TargetDocumentId,
    int RelationTypeId);

/// <summary>
/// The documents reachable from a root through links the caller may see. Links to documents the caller
/// cannot view are never followed; <paramref name="HiddenRelationCount"/> only counts them.
/// <paramref name="Truncated"/> is true when the node limit stopped the walk early.
/// </summary>
public sealed record RelationGraphResponse(
    Guid RootDocumentId,
    IReadOnlyList<RelationGraphNode> Nodes,
    IReadOnlyList<RelationGraphEdge> Edges,
    IReadOnlyList<RelationTypeResponse> Types,
    int HiddenRelationCount,
    bool Truncated);

/// <summary>One entry of the permanent relation history, which also covers links since removed.</summary>
public sealed record DocumentRelationEventResponse(
    Guid RelationEventId,
    Guid RelationId,
    Guid SourceDocumentId,
    Guid TargetDocumentId,
    int RelationTypeId,
    int ActionTypeId,
    Guid ActorUserId,
    string? Reason,
    DateTime OccurredAt)
{
    public static DocumentRelationEventResponse From(DocumentRelationEvent relationEvent) => new(
        relationEvent.RelationEventId,
        relationEvent.RelationId,
        relationEvent.SourceDocumentId,
        relationEvent.TargetDocumentId,
        relationEvent.RelationTypeId,
        relationEvent.ActionTypeId,
        relationEvent.ActorUserId,
        relationEvent.Reason,
        relationEvent.OccurredAt);
}
