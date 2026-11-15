namespace InstitutionalDiwan.Application.Abstractions.Authentication;

/// <summary>
/// Counts failed login attempts per username and locks an account out for a short window after
/// too many consecutive failures, so online password guessing is throttled even when attempts
/// come from many different addresses. Attempts are counted for unknown usernames too, otherwise
/// the lockout itself would become an account-enumeration oracle.
/// </summary>
public interface ILoginAttemptTracker
{
    /// <summary>True when the username is currently locked out.</summary>
    Task<bool> IsLockedOutAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records one failed attempt. Returns true when this failure pushed the username into a
    /// lockout, so the caller can tell the client immediately.
    /// </summary>
    Task<bool> RegisterFailureAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Clears the failure count and lockout for the username after a successful login.</summary>
    Task ResetAsync(string username, CancellationToken cancellationToken = default);
}
