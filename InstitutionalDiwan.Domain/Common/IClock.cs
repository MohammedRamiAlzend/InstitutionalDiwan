namespace InstitutionalDiwan.Domain.Common;

/// <summary>
/// Supplies "now" as a dependency instead of reading the system clock statically, so
/// time-dependent domain and application behaviour can be exercised at a fixed instant.
/// The production implementation is <c>InstitutionalDiwan.Infrastructure.Time.SystemClock</c>.
/// </summary>
public interface IClock
{
    /// <summary>The current instant, in UTC.</summary>
    DateTime UtcNow { get; }
}
