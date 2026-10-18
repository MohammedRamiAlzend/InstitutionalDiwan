using System;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Domain.Entities.Relations;

public sealed class DocumentRelationEvent : Entity<Guid>
{
    public Guid RelationEventId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → DocumentRelation.RelationId (InstitutionalDiwan.Domain.Entities.Relations.DocumentRelation)
    public Guid RelationId { get; private set; }
    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid SourceDocumentId { get; private set; }
    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid TargetDocumentId { get; private set; }
    // FK → DocumentRelationType.Code (InstitutionalDiwan.Domain.Entities.Relations.DocumentRelationType)
    public string RelationTypeCode { get; private set; } = null!;
    public string ActionType { get; private set; } = null!;
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid ActorUserId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime OccurredAt { get; private set; }

    private DocumentRelationEvent()
    {
    }

    public static Result<DocumentRelationEvent> Record(
        Guid relationId,
        Guid sourceDocumentId,
        Guid targetDocumentId,
        string relationTypeCode,
        string actionType,
        Guid actorUserId,
        string? reason = null)
    {
        var ev = new DocumentRelationEvent
        {
            RelationEventId = Guid.NewGuid(),
            RelationId = relationId,
            SourceDocumentId = sourceDocumentId,
            TargetDocumentId = targetDocumentId,
            RelationTypeCode = relationTypeCode.Trim().ToUpperInvariant(),
            ActionType = actionType.Trim().ToUpperInvariant(),
            ActorUserId = actorUserId,
            Reason = reason,
            OccurredAt = DateTime.UtcNow
        };

        return Result.Success(ev);
    }
}
