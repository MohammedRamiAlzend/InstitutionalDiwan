using InstitutionalDiwan.Application.Abstractions.Authentication;

namespace InstitutionalDiwan.Infrastructure.Persistence.Seed;

/// <summary>Shared services handed to every <see cref="ISeeder"/> execution.</summary>
public sealed class SeederContext
{
    public SeederContext(DiwanDbContext dbContext, IPasswordHasher passwordHasher, string environmentName)
    {
        Db = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        PasswordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        EnvironmentName = environmentName ?? throw new ArgumentNullException(nameof(environmentName));
    }

    /// <summary>The database context the seeder writes to.</summary>
    public DiwanDbContext Db { get; }

    /// <summary>Hasher used by seeders that create credentials.</summary>
    public IPasswordHasher PasswordHasher { get; }

    /// <summary>Current host environment name (Development, Staging, Production, ...).</summary>
    public string EnvironmentName { get; }
}
