using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Seed;

public sealed class SeederRunnerTests
{
    private sealed class RecordingSeeder : ISeeder
    {
        private readonly List<string> _log;
        private readonly string _name;

        public RecordingSeeder(int order, string name, List<string> log, params string[] environments)
        {
            Order = order;
            _name = name;
            _log = log;
            Environments = environments.Length > 0 ? environments : new[] { SeedingEnvironments.All };
        }

        public int Order { get; }

        public IReadOnlyCollection<string> Environments { get; }

        public Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
        {
            _log.Add(_name);
            return Task.CompletedTask;
        }
    }

    private sealed class AaaTieSeeder : ISeeder
    {
        private readonly List<string> _log;

        public AaaTieSeeder(List<string> log) => _log = log;

        public int Order => 100;

        public IReadOnlyCollection<string> Environments { get; } = new[] { SeedingEnvironments.All };

        public Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
        {
            _log.Add(nameof(AaaTieSeeder));
            return Task.CompletedTask;
        }
    }

    private sealed class ZzzTieSeeder : ISeeder
    {
        private readonly List<string> _log;

        public ZzzTieSeeder(List<string> log) => _log = log;

        public int Order => 100;

        public IReadOnlyCollection<string> Environments { get; } = new[] { SeedingEnvironments.All };

        public Task SeedAsync(SeederContext context, CancellationToken cancellationToken = default)
        {
            _log.Add(nameof(ZzzTieSeeder));
            return Task.CompletedTask;
        }
    }

    private static Task<int> RunAsync(string environment, params ISeeder[] seeders)
        => SeederRunner.RunAsync(
            seeders,
            new SeederContext(TestContextFactory.Create(), new FakePasswordHasher(), environment));

    [Fact]
    public async Task RunAsync_ExecutesSeedersInAscendingOrder()
    {
        var log = new List<string>();
        var seeders = new ISeeder[]
        {
            new RecordingSeeder(300, "third", log),
            new RecordingSeeder(100, "first", log),
            new RecordingSeeder(200, "second", log),
        };

        var executed = await RunAsync(SeedingEnvironments.Development, seeders);

        Assert.Equal(new[] { "first", "second", "third" }, log);
        Assert.Equal(3, executed);
    }

    [Fact]
    public async Task RunAsync_BreaksOrderTiesByTypeName()
    {
        var log = new List<string>();

        var executed = await RunAsync(SeedingEnvironments.Development, new ZzzTieSeeder(log), new AaaTieSeeder(log));

        Assert.Equal(new[] { nameof(AaaTieSeeder), nameof(ZzzTieSeeder) }, log);
        Assert.Equal(2, executed);
    }

    [Fact]
    public async Task RunAsync_SkipsSeederWhenEnvironmentDoesNotMatch()
    {
        var log = new List<string>();

        var executed = await RunAsync(
            SeedingEnvironments.Production,
            new RecordingSeeder(100, "dev-only", log, SeedingEnvironments.Development));

        Assert.Equal(0, executed);
        Assert.Empty(log);
    }

    [Theory]
    [InlineData(SeedingEnvironments.Development)]
    [InlineData(SeedingEnvironments.Staging)]
    [InlineData(SeedingEnvironments.Production)]
    public async Task RunAsync_WildcardSeederRunsInEveryEnvironment(string environment)
    {
        var log = new List<string>();

        var executed = await RunAsync(
            environment,
            new RecordingSeeder(100, "any", log, SeedingEnvironments.All));

        Assert.Equal(1, executed);
        Assert.Single(log);
    }

    [Fact]
    public async Task RunAsync_MatchesEnvironmentCaseInsensitively()
    {
        var log = new List<string>();

        var executed = await RunAsync(
            "production",
            new RecordingSeeder(100, "prod", log, SeedingEnvironments.Production));

        Assert.Equal(1, executed);
        Assert.Single(log);
    }

    [Fact]
    public async Task RunAsync_CountsOnlySeedersThatRan()
    {
        var log = new List<string>();
        var seeders = new ISeeder[]
        {
            new RecordingSeeder(100, "all", log, SeedingEnvironments.All),
            new RecordingSeeder(200, "dev", log, SeedingEnvironments.Development),
            new RecordingSeeder(300, "staging", log, SeedingEnvironments.Staging),
        };

        var executed = await RunAsync(SeedingEnvironments.Development, seeders);

        Assert.Equal(2, executed);
    }
}
