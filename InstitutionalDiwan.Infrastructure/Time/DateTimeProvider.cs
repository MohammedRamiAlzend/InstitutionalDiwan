using InstitutionalDiwan.Application.Abstractions.Time;
using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Infrastructure.Time;

/// <summary>
/// Production implementation of <see cref="IDateTimeProvider"/>. It reads the shared
/// <see cref="IClock"/> and applies the institution's <c>Asia/Damascus</c> zone (with the Windows
/// fallback id for hosts that do not yet resolve IANA ids).
/// </summary>
internal sealed class DateTimeProvider : IDateTimeProvider
{
    private static readonly TimeZoneInfo InstitutionTimeZone = ResolveInstitutionTimeZone();

    private readonly IClock _clock;

    public DateTimeProvider(IClock clock) => _clock = clock;

    public DateTime UtcNow => _clock.UtcNow;

    public DateTime ToInstitutionLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(AsUtc(utc), InstitutionTimeZone);

    public int NumberingYear(DateTime utc) => ToInstitutionLocal(utc).Year;

    private static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    private static TimeZoneInfo ResolveInstitutionTimeZone()
    {
        foreach (var id in new[] { "Asia/Damascus", "Syria Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }
}
