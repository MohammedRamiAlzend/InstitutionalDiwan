using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>
/// A fixed <see cref="IClock"/> so a test never depends on the wall clock. Time-stamped domain
/// behaviour can then be asserted exactly, and the suite stays deterministic.
/// </summary>
internal sealed class TestClock : IClock
{
    /// <summary>The default instant, 2026-01-01T00:00:00Z.</summary>
    public static readonly TestClock Instance = new(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

    public TestClock(DateTime utcNow) => UtcNow = utcNow;

    public DateTime UtcNow { get; }
}
