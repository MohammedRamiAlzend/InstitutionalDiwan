using System;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Relations;

public sealed class DocumentRelation : AggregateRoot<Guid>
{
    public Guid RelationId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid SourceDocumentId { get; private set; }
    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid TargetDocumentId { get; private set; }
    // FK → DocumentRelationType.Code (InstitutionalDiwan.Domain.Entities.Relations.DocumentRelationType)
    public string RelationTypeCode { get; private set; } = null!;
    public string? Notes { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid CreatedByUserId { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private DocumentRelation()
    {
    }

    public static Result<DocumentRelation> Create(
        Guid sourceDocumentId,
        Guid targetDocumentId,
        DocumentRelationType relationType,
        Guid createdByUserId,
        bool wouldIntroduceCycle = false,
        string? notes = null)
    {
        if (sourceDocumentId == targetDocumentId)
            return Result.Failure<DocumentRelation>(DomainErrors.Relations.SelfRelation);

        if (!relationType.IsActive)
            return Result.Failure<DocumentRelation>(DomainErrors.Relations.RelationTypeInactive);

        if (relationType.PreventCycles && wouldIntroduceCycle)
            return Result.Failure<DocumentRelation>(DomainErrors.Relations.CycleDetected);

        var (normalizedSource, normalizedTarget) = relationType.NormalizeEndpoints(sourceDocumentId, targetDocumentId);

        var relation = new DocumentRelation
        {
            RelationId = Guid.NewGuid(),
            SourceDocumentId = normalizedSource,
            TargetDocumentId = normalizedTarget,
            RelationTypeCode = relationType.Code,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        relation.RaiseDomainEvent(new DocumentRelationCreatedEvent(
            relation.RelationId,
            relation.SourceDocumentId,
            relation.TargetDocumentId,
            relation.RelationTypeCode,
            createdByUserId));

        return Result.Success(relation);
    }
}
