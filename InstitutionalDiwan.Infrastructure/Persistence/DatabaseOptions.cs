using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace InstitutionalDiwan.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    public string Provider { get; init; } = "SqlServer";
}

public interface IDatabaseProviderSelector
{
    string Provider { get; }
}

public sealed class DatabaseProviderSelector : IDatabaseProviderSelector
{
    public string Provider { get; }
    public DatabaseProviderSelector(IOptions<DatabaseOptions> options) =>
        Provider = options.Value.Provider;
}
