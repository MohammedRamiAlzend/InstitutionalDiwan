using InstitutionalDiwan.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>
/// Creates <see cref="DiwanDbContext"/> instances backed by a private SQLite in-memory database.
/// The database lives only while the connection handed to the context stays open, so every test
/// is isolated and nothing persists after the process exits.
/// </summary>
internal static class TestContextFactory
{
    public static DiwanDbContext Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DiwanDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new DiwanDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
