using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Tests.Support;
using System.Security.Cryptography;

namespace InstitutionalDiwan.Tests.Application;

/// <summary>
/// Module 11 Phase 5 security regressions: name injection rejected through the application
/// handlers (not just the domain factory), file bytes / presigned download URLs / version lists
/// withheld from unauthorized callers, cross-file version isolation, and pending-version bytes
/// never served. Presigned key-traversal, JWT and signature checks live in the S3 / OnlyOffice /
/// upload-guard suites; these cover the handler-level access surface.
/// </summary>
public sealed class DriveSecurityTests
{
    private const long DefaultQuota = 5L * 1024 * 1024 * 1024;

    [Theory]
    [InlineData("../../evil.docx")]
    [InlineData("a/b.docx")]
    [InlineData("a\\b.docx")]
    public async Task InitiateUpload_TraversalName_Rejected(string name)
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var result = await world.InitiateUpload(world.World.Owner).Handle(
            new InitiateDriveUploadCommand(world.World.Personal.SpaceId, null, name, 1024), CancellationToken.None);
        Assert.Equal("Drive.ItemNameInvalid", result.Error.Code);
    }

    [Fact]
    public async Task Rename_ToForbiddenName_Rejected()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var result = await world.RenameItem(world.World.Owner).Handle(
            new RenameDriveItemCommand(world.World.RootFolder.ItemId, "../escape"), CancellationToken.None);
        Assert.Equal("Drive.ItemNameInvalid", result.Error.Code);
    }

    [Fact]
    public async Task CreateFolder_TraversalName_Rejected()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var result = await world.CreateFolder(world.World.Owner).Handle(
            new CreateDriveFolderCommand(world.World.Shared.SpaceId, null, ".."), CancellationToken.None);
        Assert.Equal("Drive.ItemNameInvalid", result.Error.Code);
    }

    [Fact]
    public async Task FileReads_Stranger_GetsNoBytesUrlOrVersions()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        await SeedCommittedFileAsync(world, file, new byte[1024]);
        var stranger = world.World.Stranger;
        var ct = CancellationToken.None;

        var content = await world.FileContent(stranger).Handle(new GetDriveFileContentQuery(file.ItemId), ct);
        Assert.Equal("Drive.AccessDenied", content.Error.Code);

        var url = await world.DownloadUrl(stranger).Handle(new GetDriveDownloadUrlQuery(file.ItemId), ct);
        Assert.Equal("Drive.AccessDenied", url.Error.Code);

        var versions = await world.FileVersions(stranger).Handle(new ListDriveFileVersionsQuery(file.ItemId), ct);
        Assert.Equal("Drive.AccessDenied", versions.Error.Code);
    }

    [Fact]
    public async Task FileContent_VersionOfAnotherFile_Rejected()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var target = world.World.DeepFile;
        var other = world.World.MemberFile;
        await SeedCommittedFileAsync(world, target, new byte[1024]);
        var otherVersion = await SeedCommittedFileAsync(world, other, new byte[512]);
        var ct = CancellationToken.None;

        // The caller may view the target, but must not reach another file's version through it.
        var content = await world.FileContent(world.World.Owner).Handle(
            new GetDriveFileContentQuery(target.ItemId, otherVersion.VersionId), ct);
        Assert.Equal("Drive.VersionNotFound", content.Error.Code);

        var url = await world.DownloadUrl(world.World.Owner).Handle(
            new GetDriveDownloadUrlQuery(target.ItemId, otherVersion.VersionId), ct);
        Assert.Equal("Drive.VersionNotFound", url.Error.Code);
    }

    [Fact]
    public async Task FileContent_PendingVersion_NotServed()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        await SeedCommittedFileAsync(world, file, new byte[1024]);
        var pending = await world.World.AddVersionAsync(file, 2, commit: false);
        var ct = CancellationToken.None;

        var content = await world.FileContent(world.World.Owner).Handle(
            new GetDriveFileContentQuery(file.ItemId, pending.VersionId), ct);
        Assert.Equal("Drive.VersionNotFound", content.Error.Code);
    }

    [Fact]
    public async Task Viewer_CanReadButNotEdit()
    {
        var world = await DriveHandlerWorld.CreateAsync();
        var file = world.World.DeepFile;
        await SeedCommittedFileAsync(world, file, new byte[1024]);
        await world.World.AddGrantAsync(file.ItemId, DrivePrincipalType.UserId, world.World.Member.UserId, DriveRole.ViewerId);
        var ct = CancellationToken.None;

        var url = await world.DownloadUrl(world.World.Member).Handle(new GetDriveDownloadUrlQuery(file.ItemId), ct);
        Assert.True(url.IsSuccess);
        Assert.Contains("verb=GET", url.Value.Url.ToString());

        var rename = await world.RenameItem(world.World.Member).Handle(new RenameDriveItemCommand(file.ItemId, "جديد.docx"), ct);
        Assert.Equal("Drive.AccessDenied", rename.Error.Code);
    }

    private static async Task<DriveFileVersion> SeedCommittedFileAsync(DriveHandlerWorld world, DriveItem file, byte[] content)
    {
        var checksum = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        var objectKey = $"{file.SpaceId}/{file.ItemId}/1";
        var version = DriveFileVersion.CreatePending(
            file.ItemId, 1, FileStorageProvider.DriveSeaweedFsId, objectKey, content.LongLength,
            "text/plain", DriveVersionSource.UploadId, world.World.Owner.UserId).Value;
        version.Commit(checksum, content.LongLength);
        world.World.Db.DriveFileVersions.Add(version);
        file.SetCurrentVersion(version.VersionId, 1, content.LongLength, checksum);
        var space = await world.World.Spaces.GetByIdAsync(file.SpaceId);
        space!.AddUsage(content.LongLength, DefaultQuota);
        await world.World.Db.SaveChangesAsync();
        world.Storage.PutObject(objectKey, content);
        return version;
    }
}