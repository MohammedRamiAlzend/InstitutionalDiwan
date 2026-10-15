using InstitutionalDiwan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>Creates <see cref="DiwanDbContext"/> instances backed by isolated in-memory databases.</summary>
internal static class TestContextFactory
{
    public static DiwanDbContext Create()
    {
        var options = new DbContextOptionsBuilder<DiwanDbContext>()
            .UseInMemoryDatabase($"diwan-tests-{Guid.NewGuid():N}")
            .Options;

        var context = new DiwanDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
