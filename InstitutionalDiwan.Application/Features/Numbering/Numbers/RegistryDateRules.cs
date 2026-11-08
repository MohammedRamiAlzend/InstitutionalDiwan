using InstitutionalDiwan.Domain.Common;

namespace InstitutionalDiwan.Application.Features.Numbering.Numbers;

/// <summary>Validation shared by every place a clerk writes a date in a department register.</summary>
internal static class RegistryDateRules
{
    /// <summary>
    /// True when the date is after today. One day of slack, because the clerk's local date can be
    /// ahead of UTC.
    /// </summary>
    public static bool IsFuture(DateTime registryDate, IClock clock) =>
        registryDate.Date > clock.UtcNow.Date.AddDays(1);
}
