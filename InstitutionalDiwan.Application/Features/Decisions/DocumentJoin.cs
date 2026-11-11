using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Features.Relations;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Relations;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Decisions;

/// <summary>
/// Unification is a decision on the joined file plus one <c>JoinedInto</c> link to the principal file.
/// The link is created and removed through the relation code, in the decision's own save.
/// </summary>
internal static class DocumentJoin
{
    public static bool NamesATarget(Guid? joinTargetDocumentId) =>
        joinTargetDocumentId is { } targetId && targetId != Guid.Empty;

    public static async Task<bool> IsJoinedAsync(
        IDocumentRelationRepository relations,
        Guid documentId,
        CancellationToken cancellationToken) =>
        (await relations.ListOutgoingAsync(documentId, DocumentRelationType.JoinedIntoId, cancellationToken)).Count > 0;

    public static async Task<Guid?> PrincipalIdAsync(
        IDocumentRelationRepository relations,
        Guid documentId,
        CancellationToken cancellationToken)
    {
        var links = await relations.ListOutgoingAsync(
            documentId, DocumentRelationType.JoinedIntoId, cancellationToken);
        return links.Count == 0 ? null : links[0].TargetDocumentId;
    }

    /// <summary>
    /// Links <paramref name="joinedDocumentId"/> into <paramref name="principalDocumentId"/>.
    /// Every refusal is <see cref="DomainErrors.Decisions.JoinTargetInvalid"/>.
    /// </summary>
    public static async Task<Result> LinkPrincipalAsync(
        IDocumentRepository documents,
        IDocumentRelationRepository relations,
        IDocumentAccessEvaluator access,
        Guid joinedDocumentId,
        Guid principalDocumentId,
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (principalDocumentId == Guid.Empty || principalDocumentId == joinedDocumentId)
            return Result.Failure(DomainErrors.Decisions.JoinTargetInvalid);

        var principal = await documents.GetByIdAsync(principalDocumentId, cancellationToken);
        if (principal is null)
            return Result.Failure(DomainErrors.Decisions.JoinTargetInvalid);

        if (!await access.CanAccessDocumentAsync(principalDocumentId, AccessLevel.ViewId, cancellationToken))
            return Result.Failure(DomainErrors.Decisions.JoinTargetInvalid);

        if (principal.CurrentStatusId is DocumentStatusType.ArchivedId or DocumentStatusType.CanceledId)
            return Result.Failure(DomainErrors.Decisions.JoinTargetInvalid);

        if (await IsJoinedAsync(relations, principalDocumentId, cancellationToken))
            return Result.Failure(DomainErrors.Decisions.JoinTargetInvalid);

        var type = await relations.GetTypeByIdAsync(DocumentRelationType.JoinedIntoId, cancellationToken);
        if (type is null || !type.IsActive)
            return Result.Failure(DomainErrors.Decisions.JoinTargetInvalid);

        var linked = await DocumentRelationLinking.LinkAsync(
            relations,
            type,
            joinedDocumentId,
            principalDocumentId,
            userId,
            notes: null,
            utcNow,
            cancellationToken);
        return linked.IsFailure
            ? Result.Failure(DomainErrors.Decisions.JoinTargetInvalid)
            : Result.Success();
    }

    public static async Task<Result> RemoveLinksAsync(
        IDocumentRelationRepository relations,
        Guid documentId,
        Guid userId,
        string reason,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var links = await relations.ListOutgoingAsync(
            documentId, DocumentRelationType.JoinedIntoId, cancellationToken);
        if (links.Count == 0)
            return Result.Success();

        var relationReason = reason.Trim();
        if (relationReason.Length > DocumentRelation.MaxReasonLength)
            relationReason = relationReason[..DocumentRelation.MaxReasonLength];

        foreach (var link in links)
        {
            var removed = DocumentRelationLinking.Unlink(relations, link, userId, relationReason, utcNow);
            if (removed.IsFailure)
                return removed;
        }

        return Result.Success();
    }
}
