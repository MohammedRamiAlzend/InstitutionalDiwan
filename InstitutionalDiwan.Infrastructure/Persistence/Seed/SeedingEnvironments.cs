namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Well-known host environment names understood by <see cref="ISeeder.Environments"/>.</summary>
public static class SeedingEnvironments
{
    /// <summary>Wildcard: the seeder runs in every host environment.</summary>
    public const string All = "All";

    public const string Development = "Development";
    public const string Staging = "Staging";
    public const string Production = "Production";
}
