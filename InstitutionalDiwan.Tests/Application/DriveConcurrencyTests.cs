using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Infrastructure.Persistence;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Tests.Support;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>
/// Module 11 Phase 5 light concurrent-load tests: the space quota counter under racing uploads,
/// version numbering under a burst of editor saves, and the database unique index that rejects a
/// duplicate version number allocated by two concurrent contexts over the same database.
/// </summary>
public sealed class DriveConcurrencyTests
{
    private const long DefaultQuota = 5L * 1024 * 1024 * 1024;
    private const string WordMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    [Fact]
    public async Task Quota_RacingInitiates_CompleteTimeRecheckPreventsOverAllocation()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var owner = world.World.Owner;
        var ct = CancellationToken.None;
        await world.SetQuota(world.World.Admin, isAdmin: true).Handle(
            new SetDriveSpaceQuotaCommand(world.World.Personal.SpaceId, 1500), ct);

        // Two uploads race: both initiate against the same empty usage, so both reserve a slot…
        var a = await world.InitiateUpload(owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "أ.docx", 1024, WordMime), ct);
        var b = await world.InitiateUpload(owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "ب.docx", 1024, WordMime), ct);
        Assert.True(a.IsSuccess);
        Assert.True(b.IsSuccess);
        world.Storage.PutObject(a.Value.ObjectKey, new byte[1024]);
        world.Storage.PutObject(b.Value.ObjectKey, new byte[1024]);

        // …but the complete-time quota re-check lets only the first commit; the second would exceed.
        var completeA = await world.CompleteUpload(owner).Handle(new CompleteDriveUploadCommand(a.Value.VersionId), ct);
        Assert.True(completeA.IsSuccess);
        var completeB = await world.CompleteUpload(owner).Handle(new CompleteDriveUploadCommand(b.Value.VersionId), ct);
        Assert.Equal("Drive.QuotaExceeded", completeB.Error.Code);

        var space = await world.World.Spaces.GetByIdAsync(world.World.Personal.SpaceId);
        Assert.Equal(1024, space!.UsedBytes); // never over-allocated
        Assert.Equal(DriveVersionStatus.PendingId, (await world.World.Versions.GetByIdAsync(b.Value.VersionId))!.StatusId);
    }

    [Fact]
    public async Task Versions_BurstOfEditorSaves_UniqueSequentialNumbersAndExactUsage()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var owner = world.World.Owner;
        var ct = CancellationToken.None;

        var initiated = await world.InitiateUpload(owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, "مستند.docx", 1024, WordMime), ct);
        Assert.True(initiated.IsSuccess);
        var fileId = initiated.Value.ItemId;
        world.Storage.PutObject(initiated.Value.ObjectKey, new byte[1024]);
        Assert.True((await world.CompleteUpload(owner).Handle(new CompleteDriveUploadCommand(initiated.Value.VersionId), ct)).IsSuccess);

        const int saves = 20;
        for (var i = 1; i <= saves; i++)
        {
            var config = await world.EditConfig(owner).Handle(new GetDriveEditConfigQuery(fileId), ct);
            Assert.True(config.IsSuccess);
            var document = (IReadOnlyDictionary<string, object?>)config.Value.Config["document"]!;
            var key = Assert.IsType<string>(document["key"]);
            world.Downloader.Bytes = Enumerable.Repeat((byte)i, 1024 + i).ToArray();
            var token = world.Tokens.Sign(new Dictionary<string, object?> { ["key"] = key, ["status"] = 2, ["url"] = "https://onlyoffice.local/tmp/save" });
            var save = await world.Callback().Handle(
                new ProcessDriveOnlyOfficeCallbackCommand(new DriveOnlyOfficeCallbackPayload(key, 2, "https://onlyoffice.local/tmp/save", null, null, token)), ct);
            Assert.Equal(0, save.Value.Error);
        }

        var versions = await world.World.Versions.ListByItemAsync(fileId);
        Assert.Equal(saves + 1, versions.Count);
        Assert.Equal(Enumerable.Range(1, saves + 1), versions.OrderBy(v => v.VersionNumber).Select(v => v.VersionNumber));
        Assert.All(versions, v => Assert.True(v.IsCommitted));
        var file = await world.World.Items.GetByIdAsync(fileId);
        Assert.Equal(versions.Single(v => v.VersionNumber == saves + 1).VersionId, file!.CurrentVersionId);
        var space = await world.World.Spaces.GetByIdAsync(world.World.Personal.SpaceId);
        Assert.Equal(versions.Sum(v => v.SizeBytes), space!.UsedBytes);
    }

    [Fact]
    public async Task VersionNumber_ConcurrentAllocation_UniqueIndexRejectsDuplicate()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        await world.World.AddVersionAsync(file, 1, commit: true);
        await world.World.Db.SaveChangesAsync();

        // A second context over the SAME in-memory database stands in for a concurrent request.
        var connection = world.World.Db.Database.GetDbConnection();
        var options = new DbContextOptionsBuilder<DiwanDbContext>().UseSqlite(connection).Options;
        await using var other = new DiwanDbContext(options);

        var nextA = await world.World.Versions.GetNextVersionNumberAsync(file.ItemId);
        var nextB = await new DriveFileVersionRepository(other).GetNextVersionNumberAsync(file.ItemId);
        Assert.Equal(2, nextA);
        Assert.Equal(nextA, nextB); // both read MAX+1 before either inserts

        var versionA = DriveFileVersion.CreatePending(
            file.ItemId, nextA, FileStorageProvider.DriveSeaweedFsId, $"{file.SpaceId}/{file.ItemId}/{nextA}",
            1024, "text/plain", DriveVersionSource.UploadId, world.World.Owner.UserId).Value;
        world.World.Db.DriveFileVersions.Add(versionA);
        await world.World.Db.SaveChangesAsync(); // first writer wins

        var versionB = DriveFileVersion.CreatePending(
            file.ItemId, nextB, FileStorageProvider.DriveSeaweedFsId, $"{file.SpaceId}/{file.ItemId}/{nextB}-b",
            1024, "text/plain", DriveVersionSource.UploadId, world.World.Owner.UserId).Value;
        other.DriveFileVersions.Add(versionB);
        // UX_DRIVE_VERSIONS_NO (FileItemId, VersionNumber) rejects the duplicate allocation.
        await Assert.ThrowsAsync<DbUpdateException>(() => other.SaveChangesAsync());

        Assert.Equal(2, (await world.World.Versions.ListByItemAsync(file.ItemId)).Count);
    }
}