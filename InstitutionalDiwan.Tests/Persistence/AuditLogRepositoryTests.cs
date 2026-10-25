using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Repositories;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Persistence;

public sealed class AuditLogRepositoryTests
{
    private static AuditLog NewLog(string previousHash, string module = "DOCUMENTS", Guid? actor = null)
    {
        var result = AuditLog.Record(
            module,
            AuditActionType.UpdateId,
            AuditEntityType.DocumentId,
            Guid.NewGuid().ToString(),
            previousHash,
            actor,
            occurredAtUtc: new DateTime(2026, 2, 3, 4, 5, 6, 789, DateTimeKind.Utc),
            newValuesJson: "{\"value\":1}");
        Assert.True(result.IsSuccess);
        return result.Value;
    }

    private static async Task<(DiwanDbContext Db, AuditLog First, AuditLog Second)> SeedAsync()
    {
        var db = TestContextFactory.Create();
        TestContextFactory.SeedAuditLookups(db);
        var first = NewLog(AuditLog.GenesisHash, actor: Guid.NewGuid());
        var second = NewLog(first.EventHash, "IDENTITY");
        var third = NewLog(second.EventHash, "IDENTITY");

        db.AuditLogs.AddRange(first, second, third);
        await db.SaveChangesAsync();

        return (db, first, second);
    }

    [Fact]
    public async Task ListRange_PreservesChainAcrossDatabaseRoundTrip()
    {
        var (db, _, _) = await SeedAsync();
        using var _db = db;
        var repository = new AuditLogRepository(db);

        var logs = await repository.ListRangeAsync(1, 3);

        Assert.Equal(3, logs.Count);
        Assert.Equal(AuditLog.GenesisHash, logs[0].PreviousHash);
        Assert.Equal(logs[0].EventHash, logs[1].PreviousHash);
        Assert.Equal(logs[1].EventHash, logs[2].PreviousHash);

        var expected = AuditLog.GenesisHash;
        foreach (var log in logs)
        {
            Assert.True(log.VerifyIntegrity(expected).IsSuccess);
            expected = log.EventHash;
        }
    }

    [Fact]
    public async Task SearchAsync_FiltersByModuleAndActor()
    {
        var (db, first, _) = await SeedAsync();
        using var _db = db;
        var repository = new AuditLogRepository(db);

        var byModule = await repository.SearchAsync(
            new AuditLogFilter(ModuleName: "identity"),
            new PageRequest());

        Assert.Equal(2, byModule.TotalCount);
        Assert.All(byModule.Items, log => Assert.Equal("IDENTITY", log.ModuleName));

        var byActor = await repository.SearchAsync(
            new AuditLogFilter(ActorUserId: first.ActorUserId),
            new PageRequest());

        Assert.Equal(1, byActor.TotalCount);
        Assert.Equal(first.AuditLogId, byActor.Items.Single().AuditLogId);
    }

    [Fact]
    public async Task GetLatestAsync_ReturnsLastEntry()
    {
        var (db, _, _) = await SeedAsync();
        using var _db = db;
        var repository = new AuditLogRepository(db);

        var latest = await repository.GetLatestAsync();

        Assert.NotNull(latest);
        Assert.Equal(3, latest.AuditLogId);
    }

    [Fact]
    public async Task PreviousHash_IsUnique_SoTheChainCannotFork()
    {
        using var db = TestContextFactory.Create();
        TestContextFactory.SeedAuditLookups(db);

        db.AuditLogs.Add(NewLog(AuditLog.GenesisHash));
        await db.SaveChangesAsync();

        db.AuditLogs.Add(NewLog(AuditLog.GenesisHash));

        await Assert.ThrowsAsync<Microsoft.EntityFrameworkCore.DbUpdateException>(
            () => db.SaveChangesAsync());
    }

    [Fact]
    public async Task SearchAsync_AppliesPagingAndNewestFirstByDefault()
    {
        var (db, _, _) = await SeedAsync();
        using var _db = db;
        var repository = new AuditLogRepository(db);

        var page = await repository.SearchAsync(
            new AuditLogFilter(), new PageRequest(Page: 1, PageSize: 2));

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(3, page.Items[0].AuditLogId);
        Assert.Equal(2, page.Items[1].AuditLogId);
    }
}
