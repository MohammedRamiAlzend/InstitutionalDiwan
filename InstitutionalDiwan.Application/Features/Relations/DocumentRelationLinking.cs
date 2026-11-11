using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Relations;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Relations;

/// <summary>
/// Creates and removes a document link, including the cycle check and the history row, without saving.
/// Callers save once so a decision and its join link share one transaction.
/// </summary>
internal static class DocumentRelationLinking
{
    public static async Task<Result<DocumentRelation>> LinkAsync(
        IDocumentRelationRepository relations,
        DocumentRelationType type,
        Guid sourceDocumentId,
        Guid targetDocumentId,
        Guid userId,
        string? notes,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (sourceDocumentId == targetDocumentId)
            return Result.Failure<DocumentRelation>(DomainErrors.Relations.SelfRelation);

        if (!type.IsActive)
            return Result.Failure<DocumentRelation>(DomainErrors.Relations.RelationTypeInactive);

        var (source, destination) = type.NormalizeEndpoints(sourceDocumentId, targetDocumentId);
        if (await relations.ExistsAsync(source, destination, type.Id, cancellationToken))
            return Result.Failure<DocumentRelation>(DomainErrors.Relations.DuplicateRelation);

        var wouldCreateCycle = type.PreventCycles && !type.IsSymmetric &&
            await relations.WouldCreateCycleAsync(type.Id, source, destination, cancellationToken);

        var relationResult = DocumentRelation.Create(
            sourceDocumentId,
            targetDocumentId,
            type,
            userId,
            wouldCreateCycle,
            notes,
            utcNow);
        if (relationResult.IsFailure)
            return Result.Failure<DocumentRelation>(relationResult.Error);

        var relation = relationResult.Value;
        var eventResult = DocumentRelationEvent.Record(
            relation.RelationId,
            relation.SourceDocumentId,
            relation.TargetDocumentId,
            relation.RelationTypeId,
            RelationActionType.CreatedId,
            userId,
            utcNow: utcNow);
        if (eventResult.IsFailure)
            return Result.Failure<DocumentRelation>(eventResult.Error);

        relations.Add(relation);
        relations.Add(eventResult.Value);
        return Result.Success(relation);
    }

    public static Result Unlink(
        IDocumentRelationRepository relations,
        DocumentRelation relation,
        Guid userId,
        string reason,
        DateTime utcNow)
    {
        var removed = relation.Remove(userId, reason);
        if (removed.IsFailure)
            return removed;

        var eventResult = DocumentRelationEvent.Record(
            relation.RelationId,
            relation.SourceDocumentId,
            relation.TargetDocumentId,
            relation.RelationTypeId,
            RelationActionType.RemovedId,
            userId,
            reason,
            utcNow);
        if (eventResult.IsFailure)
            return Result.Failure(eventResult.Error);

        relations.Add(eventResult.Value);
        relations.Remove(relation);
        return Result.Success();
    }
}
