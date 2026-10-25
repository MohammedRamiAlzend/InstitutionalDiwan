namespace InstitutionalDiwan.Application.Abstractions.Time;

/// <summary>
/// The application's single source of time. <see cref="UtcNow"/> is used for persisted timestamps,
/// while the institution-local helpers apply the configured <c>Asia/Damascus</c> zone without making
/// callers repeat the conversion.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>The current instant in UTC.</summary>
    DateTime UtcNow { get; }

    /// <summary>Converts a UTC instant to the institution's local time (Asia/Damascus).</summary>
    DateTime ToInstitutionLocal(DateTime utc);

    /// <summary>The numbering year (local calendar year) for a UTC instant.</summary>
    int NumberingYear(DateTime utc);
}
