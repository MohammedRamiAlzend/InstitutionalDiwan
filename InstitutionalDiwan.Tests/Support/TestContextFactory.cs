using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace InstitutionalDiwan.Tests.Support;

/// <summary>
/// Creates <see cref="DiwanDbContext"/> instances backed by a private SQLite in-memory database.
/// The database lives only while the connection handed to the context stays open, so every test
/// is isolated and nothing persists after the process exits.
/// </summary>
internal static class TestContextFactory
{
    public static DiwanDbContext Create(params IInterceptor[] interceptors)
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var optionsBuilder = new DbContextOptionsBuilder<DiwanDbContext>()
            .UseSqlite(connection);

        if (interceptors.Length > 0)
            optionsBuilder.AddInterceptors(interceptors);

        var context = new DiwanDbContext(optionsBuilder.Options);
        context.Database.EnsureCreated();
        return context;
    }

    /// <summary>Seeds the audit lookup FKs required before an AUDIT_LOGS row can be inserted.</summary>
    public static void SeedAuditLookups(DiwanDbContext context)
    {
        for (var id = 1; id <= 7; id++)
            context.AuditActionTypes.Add(AuditActionType.Create(id, $"Action {id}", $"إجراء {id}").Value);

        for (var id = 1; id <= 12; id++)
            context.AuditEntityTypes.Add(AuditEntityType.Create(id, $"Entity {id}", $"كيان {id}").Value);

        context.SaveChanges();
    }
}
