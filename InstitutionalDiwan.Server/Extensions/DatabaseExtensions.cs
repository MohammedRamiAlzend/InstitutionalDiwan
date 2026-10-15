using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Server.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Prepares the database at startup. For the in-memory development provider it creates the schema
    /// and seeds a demo account so the login flow can be exercised without SQL Server.
    /// </summary>
    public static async Task InitializeDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DiwanDbContext>();

        if (!context.Database.IsInMemory())
            return;

        await context.Database.EnsureCreatedAsync();

        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        await NewDiwanDbContextSeeder.SeedAsync(context, passwordHasher);
    }
}
