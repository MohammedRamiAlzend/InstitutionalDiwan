using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Infrastructure.OnlyOffice;
using InstitutionalDiwan.Tests.Support;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace InstitutionalDiwan.Tests.Application;

public sealed class DriveOnlyOfficeHandlerTests
{
    private const long DefaultQuota = 5L * 1024 * 1024 * 1024;

    [Fact]
    public async Task EditConfig_Editor_GetsEditModeAndVerifiableToken()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var (_, _, key) = await SeedEditableFileAsync(world, file, new byte[1024]);

        var result = await world.EditConfig(world.World.Owner).Handle(
            new GetDriveEditConfigQuery(file.ItemId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("edit", result.Value.Mode);
        Assert.Equal("word", result.Value.DocumentType);
        Assert.Equal("https://onlyoffice.local", result.Value.ServerUrl);
        Assert.NotEqual(Guid.Empty, result.Value.SessionId);
        Assert.Equal(key, result.Value.Config["document"] is IReadOnlyDictionary<string, object?> doc ? doc["key"] : null);
        var token = Assert.IsType<string>(result.Value.Config["token"]);
        Assert.NotNull(world.Tokens.Verify(token));
    }

    [Fact]
    public async Task EditConfig_Viewer_GetsViewMode()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        await SeedEditableFileAsync(world, file, new byte[1024]);
        await world.World.AddGrantAsync(file.ItemId, DrivePrincipalType.UserId, world.World.Member.UserId, DriveRole.ViewerId);

        var result = await world.EditConfig(world.World.Member).Handle(
            new GetDriveEditConfigQuery(file.ItemId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("view", result.Value.Mode);
        var doc = (IReadOnlyDictionary<string, object?>)result.Value.Config["document"]!;
        var permissions = (IReadOnlyDictionary<string, object?>)doc["permissions"]!;
        Assert.Equal(false, permissions["edit"]);
    }

    [Fact]
    public async Task EditConfig_ReusesOpenSessionForSameVersion()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        await SeedEditableFileAsync(world, file, new byte[1024]);

        var first = await world.EditConfig(world.World.Owner).Handle(new GetDriveEditConfigQuery(file.ItemId), CancellationToken.None);
        var second = await world.EditConfig(world.World.Owner).Handle(new GetDriveEditConfigQuery(file.ItemId), CancellationToken.None);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(first.Value.SessionId, second.Value.SessionId);
    }

    [Fact]
    public async Task EditConfig_UnsupportedType_Fails()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var video = DriveItem.CreateFile(world.World.Shared.SpaceId, null, "فيديو.mp4", world.World.Owner.UserId).Value;
        world.World.Db.DriveItems.Add(video);
        await world.World.Db.SaveChangesAsync();
        await SeedEditableFileAsync(world, video, new byte[1024]);

        var result = await world.EditConfig(world.World.Owner).Handle(new GetDriveEditConfigQuery(video.ItemId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Drive.EditingNotSupported", result.Error.Code);
    }

    [Fact]
    public async Task EditConfig_NotConfigured_Fails()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        await SeedEditableFileAsync(world, file, new byte[1024]);
        world.OnlyOfficeOptions.Enabled = false;

        var result = await world.EditConfig(world.World.Owner).Handle(new GetDriveEditConfigQuery(file.ItemId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Drive.OnlyOfficeNotConfigured", result.Error.Code);
    }

    [Fact]
    public async Task EditConfig_NoAccess_Fails()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        await SeedEditableFileAsync(world, file, new byte[1024]);

        var result = await world.EditConfig(world.World.Stranger).Handle(new GetDriveEditConfigQuery(file.ItemId), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Drive.AccessDenied", result.Error.Code);
    }

    [Fact]
    public async Task Callback_EditingStatus_KeepsSessionOpen()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var (_, session, key) = await SeedEditableFileAsync(world, file, new byte[1024]);

        var result = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(Payload(key, 1, null, world)), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.Error);
        var reloaded = await world.World.Sessions.GetByIdAsync(session.SessionId);
        Assert.True(reloaded!.IsOpen);
    }

    [Fact]
    public async Task Callback_CloseNoChange_ClosesSession()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var (_, session, key) = await SeedEditableFileAsync(world, file, new byte[1024]);

        var result = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(Payload(key, 4, null, world)), CancellationToken.None);

        Assert.Equal(0, result.Value.Error);
        var reloaded = await world.World.Sessions.GetByIdAsync(session.SessionId);
        Assert.Equal(DriveEditSessionStatus.ClosedId, reloaded!.StatusId);
    }

    [Fact]
    public async Task Callback_Save_CreatesEditorSaveVersion()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var (v1, session, key) = await SeedEditableFileAsync(world, file, new byte[1024]);
        var edited = Enumerable.Repeat((byte)0xAB, 2048).ToArray();
        world.Downloader.Bytes = edited;

        var result = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(Payload(key, 2, "https://onlyoffice.local/tmp/save", world)), CancellationToken.None);

        Assert.Equal(0, result.Value.Error);
        var versions = await world.World.Versions.ListByItemAsync(file.ItemId);
        Assert.Equal(2, versions.Count);
        var newVersion = versions.Single(v => v.VersionNumber == 2);
        Assert.Equal(DriveVersionSource.EditorSaveId, newVersion.SourceId);
        Assert.True(newVersion.IsCommitted);
        Assert.Equal(world.World.Owner.UserId, newVersion.CreatedByUserId);
        Assert.True(world.Storage.Exists(newVersion.ObjectKey));

        var reloadedFile = await world.World.Items.GetByIdAsync(file.ItemId);
        Assert.Equal(newVersion.VersionId, reloadedFile!.CurrentVersionId);
        var space = await world.World.Spaces.GetByIdAsync(file.SpaceId);
        Assert.Equal(1024 + 2048, space!.UsedBytes);
        var reloadedSession = await world.World.Sessions.GetByIdAsync(session.SessionId);
        Assert.Equal(DriveEditSessionStatus.SavedId, reloadedSession!.StatusId);
        Assert.NotEqual(v1.VersionId, reloadedFile.CurrentVersionId);
    }

    [Fact]
    public async Task Callback_ForceSave_CreatesEditorSaveVersion()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var (_, _, key) = await SeedEditableFileAsync(world, file, new byte[1024]);
        world.Downloader.Bytes = Enumerable.Repeat((byte)0xCD, 512).ToArray();

        var result = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(Payload(key, 6, "https://onlyoffice.local/tmp/force", world)), CancellationToken.None);

        Assert.Equal(0, result.Value.Error);
        var versions = await world.World.Versions.ListByItemAsync(file.ItemId);
        Assert.Equal(2, versions.Count);
        Assert.Equal(DriveVersionSource.EditorSaveId, versions.Single(v => v.VersionNumber == 2).SourceId);
    }

    [Fact]
    public async Task Callback_DuplicateChecksum_NoNewVersion()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var content = new byte[1024];
        var (_, session, key) = await SeedEditableFileAsync(world, file, content);
        world.Downloader.Bytes = content; // identical bytes → identical SHA-256

        var result = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(Payload(key, 2, "https://onlyoffice.local/tmp/save", world)), CancellationToken.None);

        Assert.Equal(0, result.Value.Error);
        var versions = await world.World.Versions.ListByItemAsync(file.ItemId);
        Assert.Single(versions);
        var reloadedSession = await world.World.Sessions.GetByIdAsync(session.SessionId);
        Assert.Equal(DriveEditSessionStatus.SavedId, reloadedSession!.StatusId);
    }

    [Fact]
    public async Task Callback_WrongSignature_Fails()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var (_, _, key) = await SeedEditableFileAsync(world, file, new byte[1024]);
        world.Downloader.Bytes = new byte[2048];
        var foreign = new OnlyOfficeTokenService(Options.Create(new DriveOnlyOfficeOptions { JwtSecret = "a-different-secret" }));
        var badToken = foreign.Sign(new Dictionary<string, object?> { ["key"] = key, ["status"] = 2 });

        var result = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(new DriveOnlyOfficeCallbackPayload(key, 2, "https://onlyoffice.local/tmp/save", null, null, badToken)), CancellationToken.None);

        Assert.Equal(1, result.Value.Error);
        var versions = await world.World.Versions.ListByItemAsync(file.ItemId);
        Assert.Single(versions);
    }

    [Fact]
    public async Task Callback_VersionConflict_RejectsAndNotifiesEditor()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var (_, _, key) = await SeedEditableFileAsync(world, file, new byte[1024]);

        // The file moves on to a second version while the session is still open against version 1.
        var v2 = await world.World.AddVersionAsync(file, 2, commit: true);
        file.SetCurrentVersion(v2.VersionId, 2, 1024, new string('a', 64));
        await world.World.Db.SaveChangesAsync();
        world.Downloader.Bytes = new byte[2048];

        var result = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(Payload(key, 2, "https://onlyoffice.local/tmp/save", world)), CancellationToken.None);

        Assert.Equal(1, result.Value.Error);
        Assert.Contains(world.Notifications.Published, n =>
            n.TypeId == NotificationType.DriveEditSaveFailedId && n.Recipients.Contains(world.World.Owner.UserId));
        var versions = await world.World.Versions.ListByItemAsync(file.ItemId);
        Assert.Equal(2, versions.Count); // no third version was created
    }

    [Fact]
    public async Task Callback_DownloadFailed_NotifiesEditor()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var (_, _, key) = await SeedEditableFileAsync(world, file, new byte[1024]);
        world.Downloader.Bytes = null;

        var result = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(Payload(key, 2, "https://onlyoffice.local/tmp/save", world)), CancellationToken.None);

        Assert.Equal(1, result.Value.Error);
        Assert.Contains(world.Notifications.Published, n => n.TypeId == NotificationType.DriveEditSaveFailedId);
        var versions = await world.World.Versions.ListByItemAsync(file.ItemId);
        Assert.Single(versions);
    }

    [Fact]
    public async Task Callback_SignatureMismatch_Fails()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var (_, _, key) = await SeedEditableFileAsync(world, file, new byte[1024]);
        world.Downloader.Bytes = new byte[2048];
        world.Signatures.Failure = DomainErrors.Attachments.SignatureMismatch;

        var result = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(Payload(key, 2, "https://onlyoffice.local/tmp/save", world)), CancellationToken.None);

        Assert.Equal(1, result.Value.Error);
        Assert.Contains(world.Notifications.Published, n => n.TypeId == NotificationType.DriveEditSaveFailedId);
        var versions = await world.World.Versions.ListByItemAsync(file.ItemId);
        Assert.Single(versions);
    }

    [Fact]
    public async Task Callback_QuotaExceeded_Fails()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        var (_, _, key) = await SeedEditableFileAsync(world, file, new byte[1024]);
        var space = await world.World.Spaces.GetByIdAsync(file.SpaceId);
        space!.SetQuota(1500);
        await world.World.Db.SaveChangesAsync();
        world.Downloader.Bytes = new byte[2048]; // 1024 used + 2048 > 1500

        var result = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(Payload(key, 2, "https://onlyoffice.local/tmp/save", world)), CancellationToken.None);

        Assert.Equal(1, result.Value.Error);
        Assert.Contains(world.Notifications.Published, n => n.TypeId == NotificationType.DriveEditSaveFailedId);
        var versions = await world.World.Versions.ListByItemAsync(file.ItemId);
        Assert.Single(versions);
    }

    [Fact]
    public async Task Callback_UnknownKey_Fails()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        await SeedEditableFileAsync(world, file, new byte[1024]);
        world.Downloader.Bytes = new byte[2048];

        var result = await world.Callback().Handle(
            new ProcessDriveOnlyOfficeCallbackCommand(Payload("no-such-key", 2, "https://onlyoffice.local/tmp/save", world)), CancellationToken.None);

        Assert.Equal(1, result.Value.Error);
    }

    private static DriveOnlyOfficeCallbackPayload Payload(string key, int status, string? url, DriveHandlerWorld world)
    {
        var token = world.Tokens.Sign(new Dictionary<string, object?> { ["key"] = key, ["status"] = status, ["url"] = url });
        return new DriveOnlyOfficeCallbackPayload(key, status, url, null, null, token);
    }

    private static async Task<(DriveFileVersion Version, DriveEditSession Session, string Key)> SeedEditableFileAsync(
        DriveHandlerWorld world, DriveItem file, byte[] content)
    {
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var objectKey = $"{file.SpaceId}/{file.ItemId}/1";
        var version = DriveFileVersion.CreatePending(
            file.ItemId, 1, FileStorageProvider.DriveSeaweedFsId, objectKey, content.LongLength,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            DriveVersionSource.UploadId, world.World.Owner.UserId).Value;
        version.Commit(checksum, content.LongLength);
        world.World.Db.DriveFileVersions.Add(version);
        file.SetCurrentVersion(version.VersionId, 1, content.LongLength, checksum);
        var space = await world.World.Spaces.GetByIdAsync(file.SpaceId);
        space!.AddUsage(content.LongLength, DefaultQuota);
        await world.World.Db.SaveChangesAsync();
        world.Storage.PutObject(objectKey, content);

        var key = $"{file.ItemId:N}-1";
        var session = DriveEditSession.Open(file.ItemId, world.World.Owner.UserId, key, version.VersionId).Value;
        world.World.Sessions.Add(session);
        await world.World.Db.SaveChangesAsync();
        return (version, session, key);
    }
}