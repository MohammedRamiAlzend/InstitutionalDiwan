namespace InstitutionalDiwan.Application.Abstractions.Authentication;

/// <summary>
/// Ambient identity of the caller for the current request, projected from the signed-in user's
/// token. Use cases and authorization evaluators depend on this instead of touching
/// <c>HttpContext</c> so they stay host-agnostic and testable.
/// </summary>
public interface IUserContext
{
    /// <summary>False when no authenticated principal is attached to the current request.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Token subject (<c>sub</c>); <see cref="System.Guid.Empty"/> when not signed in.</summary>
    Guid UserId { get; }

    /// <summary>Primary department claim (<c>dept_id</c>) — the caller's home department.</summary>
    Guid PrimaryDepartmentId { get; }

    /// <summary>Organizational scope claim (<c>scope_type</c>) referencing <c>USER_SCOPE_TYPES</c>.</summary>
    int ScopeTypeId { get; }

    /// <summary>Every permission code granted to the caller (<c>permission</c> claims).</summary>
    IReadOnlySet<string> Permissions { get; }
}
