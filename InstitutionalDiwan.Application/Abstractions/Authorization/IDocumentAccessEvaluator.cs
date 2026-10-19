namespace InstitutionalDiwan.Application.Abstractions.Authorization;

/// <summary>
/// Decides whether the caller may reach a document at a given access level. This is the third tier
/// of the three-tier access model: it combines RBAC, organizational scope, the document's
/// confidentiality level, and the per-document grants held by the caller.
/// </summary>
public interface IDocumentAccessEvaluator
{
    /// <summary>
    /// True when the caller may reach <paramref name="documentId"/> with at least
    /// <paramref name="requiredAccessLevelId"/> (see <c>AccessLevel</c>: VIEW, DOWNLOAD, FORWARD,
    /// MANAGE). Returns false for a document that does not exist, so callers can treat the answer as
    /// "not allowed" without leaking existence.
    /// </summary>
    Task<bool> CanAccessDocumentAsync(
        Guid documentId,
        int requiredAccessLevelId,
        CancellationToken cancellationToken = default);
}
