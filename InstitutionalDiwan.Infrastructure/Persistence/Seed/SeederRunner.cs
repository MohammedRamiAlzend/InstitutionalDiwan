namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Executes registered seeders ordered by <see cref="ISeeder.Order"/>, skipping any seeder whose
/// <see cref="ISeeder.Environments"/> do not include the current environment.
/// </summary>
public static class SeederRunner
{
    /// <summary>Runs every applicable seeder in order and returns how many executed.</summary>
    public static async Task<int> RunAsync(
        IEnumerable<ISeeder> seeders,
        SeederContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(seeders);
        ArgumentNullException.ThrowIfNull(context);

        var executed = 0;

        var ordered = seeders
            .OrderBy(seeder => seeder.Order)
            .ThenBy(seeder => seeder.GetType().Name, StringComparer.Ordinal);

        foreach (var seeder in ordered)
        {
            if (!seeder.RunsIn(context.EnvironmentName))
                continue;

            await seeder.SeedAsync(context, cancellationToken);
            executed++;
        }

        return executed;
    }
}
