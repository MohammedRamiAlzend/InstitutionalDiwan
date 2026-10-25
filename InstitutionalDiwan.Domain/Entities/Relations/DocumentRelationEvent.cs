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
    // FK → DocumentRelationType.Id (InstitutionalDiwan.Domain.Entities.Relations.DocumentRelationType)
    public int RelationTypeId { get; private set; }
    // FK → RelationActionType.Id (InstitutionalDiwan.Domain.Entities.Relations.RelationActionType)
    public int ActionTypeId { get; private set; }
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
        int relationTypeId,
        int actionTypeId,
        Guid actorUserId,
        string? reason = null,
        DateTime? utcNow = null)
    {
        var ev = new DocumentRelationEvent
        {
            RelationEventId = Guid.NewGuid(),
            RelationId = relationId,
            SourceDocumentId = sourceDocumentId,
            TargetDocumentId = targetDocumentId,
            RelationTypeId = relationTypeId,
            ActionTypeId = actionTypeId,
            ActorUserId = actorUserId,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            OccurredAt = utcNow ?? DateTime.UtcNow
        };

        return Result.Success(ev);
    }
}
