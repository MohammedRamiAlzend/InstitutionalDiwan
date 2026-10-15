using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Server.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Prepares the database at startup and runs the registered seeders through
    /// <see cref="SeederRunner"/>. Seeders are executed ordered by <c>Order</c> and each one
    /// decides via its own <c>Environments</c> whether it runs in Development, Production, etc.
    ///
    /// Database preparation: in-memory providers get their schema created, Development gets
    /// pending migrations applied, and other environments only seed when the database is
    /// reachable (schema management there is left to deployment tooling).
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DiwanDbContext>();
        var environmentName = app.Environment.EnvironmentName;

        if (context.Database.IsInMemory())
        {
            await context.Database.EnsureCreatedAsync();
        }
        else if (app.Environment.IsDevelopment())
        {
            await context.Database.MigrateAsync();
        }
        else if (!await context.Database.CanConnectAsync())
        {
            app.Logger.LogWarning(
                "Database is unreachable in environment {Environment}; seeding skipped.",
                environmentName);
            return;
        }

        var seedingContext = new SeederContext(
            context,
            scope.ServiceProvider.GetRequiredService<IPasswordHasher>(),
            environmentName);

        var seeders = scope.ServiceProvider.GetServices<ISeeder>().ToArray();
        var executed = await SeederRunner.RunAsync(seeders, seedingContext);

        app.Logger.LogInformation(
            "Seeding finished: {Executed} of {Total} seeder(s) executed in environment {Environment}.",
            executed,
            seeders.Length,
            environmentName);
    }
}
