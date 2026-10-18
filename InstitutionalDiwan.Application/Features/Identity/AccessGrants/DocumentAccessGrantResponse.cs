using InstitutionalDiwan.Domain.Entities.Identity;

namespace InstitutionalDiwan.Application.Features.Identity.AccessGrants;

/// <summary>Read model returned by the document access-grant endpoints.</summary>
public sealed record DocumentAccessGrantResponse(
    Guid GrantId,
    Guid DocumentId,
    Guid UserId,
    int AccessLevelId,
    Guid GrantedByUserId,
    string? Reason,
    DateTime GrantedAt,
    DateTime? ExpiresAt,
    bool IsRevoked,
    DateTime? RevokedAt,
    Guid? RevokedByUserId)
{
    public static DocumentAccessGrantResponse From(DocumentAccessGrant grant) => new(
        grant.GrantId,
        grant.DocumentId,
        grant.UserId,
        grant.AccessLevelId,
        grant.GrantedByUserId,
        grant.Reason,
        grant.GrantedAt,
        grant.ExpiresAt,
        grant.IsRevoked,
        grant.RevokedAt,
        grant.RevokedByUserId);
}
