using InstitutionalDiwan.Application.Abstractions.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace InstitutionalDiwan.Infrastructure.Authentication;

/// <summary>
/// In-memory login throttle: five failed attempts within <see cref="FailureWindow"/> lock the
/// username out for <see cref="LockoutDuration"/>. Memory-cache entries expire on their own, so
/// a restart (or cache pressure) only ever makes the tracker more permissive.
/// </summary>
public sealed class LoginAttemptTracker : ILoginAttemptTracker
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IMemoryCache _cache;

    public LoginAttemptTracker(IMemoryCache cache) => _cache = cache;

    public Task<bool> IsLockedOutAsync(string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_cache.TryGetValue(LockKey(Normalize(username)), out _));
    }

    public Task<bool> RegisterFailureAsync(string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = Normalize(username);
        var key = FailuresKey(normalized);
        var failures = _cache.TryGetValue(key, out int count) ? count : 0;
        failures++;

        if (failures >= MaxFailures)
        {
            _cache.Remove(key);
            _cache.Set(LockKey(normalized), true, LockoutDuration);
            return Task.FromResult(true);
        }

        _cache.Set(key, failures, FailureWindow);
        return Task.FromResult(false);
    }

    public Task ResetAsync(string username, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var normalized = Normalize(username);
        _cache.Remove(FailuresKey(normalized));
        _cache.Remove(LockKey(normalized));
        return Task.CompletedTask;
    }

    private static string Normalize(string username) => username.Trim().ToLowerInvariant();

    private static string FailuresKey(string username) => $"login-attempt:failures:{username}";
    private static string LockKey(string username) => $"login-attempt:lock:{username}";
}
