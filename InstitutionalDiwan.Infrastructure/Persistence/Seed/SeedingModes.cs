namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeding modes selected by the <c>Seeding:Mode</c> setting. Every seeder declares the modes it
/// runs in (<see cref="ISeeder.Modes"/>); reference data (lookups, permissions, roles, institution
/// settings) runs in every mode.
/// </summary>
public static class SeedingModes
{
    /// <summary>Wildcard: the seeder runs in every mode.</summary>
    public const string Any = "Any";

    /// <summary>The original demo data: 15-department tree, 28 employees, <c>admin</c> and <c>user</c>.</summary>
    public const string Everything = "Everything";

    /// <summary>Three diwans (central, individual, investigation), SuperAdmin and two accounts per diwan.</summary>
    public const string Standard = "Standard";

    /// <summary>Reference data and the SuperAdmin account only.</summary>
    public const string None = "None";

    public static readonly IReadOnlyCollection<string> AnyMode = new[] { Any };

    /// <summary>Parses a configured mode (case-insensitive); a missing value means <see cref="Everything"/>.</summary>
    public static string Parse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Everything;

        foreach (var mode in new[] { Everything, Standard, None })
        {
            if (string.Equals(mode, value.Trim(), StringComparison.OrdinalIgnoreCase))
                return mode;
        }

        throw new InvalidOperationException(
            $"Unknown Seeding:Mode '{value}'. Use '{Everything}', '{Standard}' or '{None}'.");
    }
}

/// <summary>Seeding settings read from the <c>Seeding</c> configuration section.</summary>
public sealed record SeedingOptions(string Mode, string DefaultPassword)
{
    /// <summary>Password given to every seeded account unless configured otherwise.</summary>
    public const string DefaultSeedPassword = "12345678";

    public static SeedingOptions Default { get; } = new(SeedingModes.Everything, DefaultSeedPassword);

    public static SeedingOptions From(string? mode, string? defaultPassword) => new(
        SeedingModes.Parse(mode),
        string.IsNullOrWhiteSpace(defaultPassword) ? DefaultSeedPassword : defaultPassword);
}
