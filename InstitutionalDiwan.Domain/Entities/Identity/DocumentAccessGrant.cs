using InstitutionalDiwan.Domain.Events;

namespace InstitutionalDiwan.Domain.Entities.Identity;

public sealed class DocumentAccessGrant : AggregateRoot<Guid>
{
    public Guid GrantId
    {
        get => Id;
        private set => Id = value;
    }

    // FK → Document.DocumentId (InstitutionalDiwan.Domain.Entities.Correspondence.Document)
    public Guid DocumentId { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid UserId { get; private set; }
    // FK → AccessLevel.Id (InstitutionalDiwan.Domain.Entities.Identity.AccessLevel)
    public int AccessLevelId { get; private set; } = AccessLevel.ViewId;
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid GrantedByUserId { get; private set; }
    public string? Reason { get; private set; }
    public DateTime GrantedAt { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTime? RevokedAt { get; private set; }
    // FK → User.UserId (InstitutionalDiwan.Domain.Entities.Identity.User)
    public Guid? RevokedByUserId { get; private set; }

    private DocumentAccessGrant()
    {
    }

    public static Result<DocumentAccessGrant> Grant(
        Guid documentId,
        Guid userId,
        int accessLevelId,
        Guid grantedByUserId,
        string? reason = null,
        DateTime? expiresAt = null)
    {
        if (accessLevelId is not (AccessLevel.ViewId or AccessLevel.DownloadId or AccessLevel.ForwardId or AccessLevel.ManageId))
            return Result.Failure<DocumentAccessGrant>(DomainErrors.Identity.AccessLevelInvalid);

        if (expiresAt.HasValue && expiresAt.Value <= DateTime.UtcNow)
            return Result.Failure<DocumentAccessGrant>(DomainErrors.Identity.InvalidDateRange);

        var grant = new DocumentAccessGrant
        {
            GrantId = Guid.NewGuid(),
            DocumentId = documentId,
            UserId = userId,
            AccessLevelId = accessLevelId,
            GrantedByUserId = grantedByUserId,
            Reason = reason,
            GrantedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            IsRevoked = false
        };

        grant.RaiseDomainEvent(new DocumentAccessGrantedEvent(grant.GrantId, grant.DocumentId, grant.UserId, grant.AccessLevelId));
        return Result.Success(grant);
    }

    public Result Revoke(Guid revokedByUserId)
    {
        if (IsRevoked)
            return Result.Failure(DomainErrors.Identity.GrantAlreadyRevoked);

        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevokedByUserId = revokedByUserId;

        RaiseDomainEvent(new DocumentAccessRevokedEvent(GrantId, DocumentId, UserId));
        return Result.Success();
    }

    public bool IsCurrentlyValid(DateTime utcNow) =>
        !IsRevoked && (!ExpiresAt.HasValue || ExpiresAt.Value > utcNow);
}
