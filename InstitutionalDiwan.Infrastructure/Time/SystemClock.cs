using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Infrastructure.Time;

/// <summary>
/// The production <see cref="IClock"/>: reads the operating system clock on every call. Registered
/// as a singleton, so there is exactly one source of "now" for the running application.
/// </summary>
internal sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
