namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// A unit of seeding work. Seeders are discovered from DI, sorted by <see cref="Order"/>
/// (ascending) and filtered by <see cref="Environments"/> before execution, so every seeder
/// declares both its position in the seeding sequence and the host environments it may run in
/// (e.g. <c>Development</c>, <c>Production</c>, or <see cref="SeedingEnvironments.All"/>).
/// Implementations must be idempotent — they run on every startup.
/// </summary>
public interface ISeeder
{
    /// <summary>Execution position; lower values run first. Use gaps of 100 for future insertions.</summary>
    int Order { get; }

    /// <summary>
    /// Host environment names this seeder runs in — e.g. <c>Development</c>, <c>Staging</c>,
    /// <c>Production</c> — or <see cref="SeedingEnvironments.All"/> to run everywhere.
    /// Comparison is case-insensitive.
    /// </summary>
    IReadOnlyCollection<string> Environments { get; }

    /// <summary>Applies the seed data. Must be safe to run repeatedly.</summary>
    Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default);
}

/// <summary>Environment-matching helpers for <see cref="ISeeder"/>.</summary>
public static class SeederExtensions
{
    /// <summary>True when this seeder is allowed to run in the given host environment name.</summary>
    public static bool RunsIn(this ISeeder seeder, string environmentName)
        => seeder.Environments.Contains(SeedingEnvironments.All, StringComparer.OrdinalIgnoreCase)
           || seeder.Environments.Contains(environmentName, StringComparer.OrdinalIgnoreCase);
}
