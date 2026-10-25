using InstitutionalDiwan.Application.Features.Attachments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Correspondence;
using InstitutionalDiwan.Domain.Entities.Identity;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Tests.Support;

namespace InstitutionalDiwan.Tests.Application;

public sealed class AttachmentHandlerTests
{
    private sealed class World
    {
        public FakeUserRepository Users { get; } = new();
        public FakeDocumentRepository Documents { get; } = new();
        public FakeDocumentFileRepository Files { get; } = new();
        public FakeDocumentFileTypeRepository FileTypes { get; } =
            new FakeDocumentFileTypeRepository().WithDefaultTypes();
        public FakeFileStorageProviderRepository Providers { get; } =
            new FakeFileStorageProviderRepository().WithLocalProvider();
        public FakeDocumentAccessEvaluator Access { get; } = new();
        public FakeFileSignatureValidator Signatures { get; } = new();
        public FakeFileStorageService Storage { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();

        public User Clerk { get; }
        public Document Document { get; }

        public World()
        {
            var department = Department.Create("DIWAN", "الديوان", 1).Value;

            var clerk = User.Create("clerk", "hash", department.DepartmentId);
            Assert.True(clerk.IsSuccess);
            Clerk = clerk.Value;
            Users.Add(Clerk);

            var document = Document.CreateDraft(
                "طلب إحاطة",
                DocumentType.InternalMemoId,
                DocumentDirection.InternalId,
                department.DepartmentId,
                Clerk.UserId);
            Assert.True(document.IsSuccess);
            Document = document.Value;
            Documents.Add(Document);
            Access.ManagedDocuments.Add(Document.DocumentId);
        }

        public AttachmentUploader Uploader() => new(Providers, Signatures, Storage);

        public CreateDocumentFileCommandHandler CreateHandler() =>
            new(Documents, FileTypes, Files, Users, Access, UnitOfWork);

        public CreateDocumentFileWithVersionCommandHandler CreateWithVersionHandler() =>
            new(Documents, FileTypes, Files, Users, Access, Uploader(), UnitOfWork);

        public UploadDocumentFileVersionCommandHandler UploadHandler() =>
            new(Files, FileTypes, Users, Access, Uploader(), UnitOfWork);

        public ArchiveDocumentFileCommandHandler ArchiveHandler() =>
            new(Files, Users, Access, UnitOfWork);

        public ListDocumentFilesQueryHandler ListHandler() =>
            new(Files, Users, Access);

        public DownloadDocumentFileVersionQueryHandler DownloadHandler() =>
            new(Files, Providers, Users, Access, Storage);

        public VerifyAttachmentChecksumQueryHandler VerifyHandler() =>
            new(Files, Providers, Users, Access, Storage);
    }

    private static async Task<DocumentFileResponse> CreateFile(World world) =>
        (await world.CreateHandler().Handle(
            new CreateDocumentFileCommand(
                world.Document.DocumentId,
                DocumentFileType.AttachmentId,
                "المرفق الأول",
                world.Clerk.UserId),
            CancellationToken.None)).Value;

    [Fact]
    public async Task CreateFile_OpensAnEmptyVersionChain()
    {
        var world = new World();

        var result = await world.CreateHandler().Handle(
            new CreateDocumentFileCommand(
                world.Document.DocumentId,
                DocumentFileType.AttachmentId,
                "المرفق الأول",
                world.Clerk.UserId,
                IsPrimaryScan: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(world.Document.DocumentId, result.Value.DocumentId);
        Assert.Equal(DocumentFileType.AttachmentId, result.Value.FileTypeId);
        Assert.Equal(0, result.Value.CurrentVersionNumber);
        Assert.Empty(result.Value.Versions);
        Assert.Single(world.Files.Files);
        Assert.Equal(1, world.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateFile_WithoutManageAccess_IsForbidden()
    {
        var world = new World();
        world.Access.ManagedDocuments.Clear();

        var result = await world.CreateHandler().Handle(
            new CreateDocumentFileCommand(
                world.Document.DocumentId,
                DocumentFileType.AttachmentId,
                "المرفق الأول",
                world.Clerk.UserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, result.Error.Code);
        Assert.Empty(world.Files.Files);
    }

    [Fact]
    public async Task CreateFile_WithAnUnknownFileType_ReturnsNotFound()
    {
        var world = new World();

        var result = await world.CreateHandler().Handle(
            new CreateDocumentFileCommand(
                world.Document.DocumentId, 999, "مرفق", world.Clerk.UserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Attachments.FileTypeNotFound.Code, result.Error.Code);
    }

    [Fact]
    public async Task CreateFileWithVersion_StoresVersion1InOneUseCase()
    {
        var world = new World();
        var bytes = "%PDF-1.7 first version"u8.ToArray();

        await using var content = new MemoryStream(bytes);
        var result = await world.CreateWithVersionHandler().Handle(
            new CreateDocumentFileWithVersionCommand(
                world.Document.DocumentId,
                DocumentFileType.ScanId,
                "النسخة الممسوحة",
                world.Clerk.UserId,
                content,
                "scan.pdf",
                "application/pdf",
                content.Length,
                IsPrimaryScan: true,
                ChangeNotes: "النسخة الأولى"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(1, result.Value.CurrentVersionNumber);
        Assert.True(result.Value.IsPrimaryScan);
        var version = Assert.Single(result.Value.Versions);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(world.Storage.Checksum, version.ChecksumSha256);
        Assert.Single(world.Files.Files);
        Assert.Equal(1, world.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateFileWithVersion_WithoutManageAccess_DoesNotStoreBytes()
    {
        var world = new World();
        world.Access.ManagedDocuments.Clear();
        await using var content = new MemoryStream("%PDF-1.7"u8.ToArray());

        var result = await world.CreateWithVersionHandler().Handle(
            new CreateDocumentFileWithVersionCommand(
                world.Document.DocumentId,
                DocumentFileType.ScanId,
                "النسخة الممسوحة",
                world.Clerk.UserId,
                content,
                "scan.pdf",
                "application/pdf",
                content.Length),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, result.Error.Code);
        Assert.Empty(world.Storage.Saved);
        Assert.Empty(world.Files.Files);
    }

    [Fact]
    public async Task UploadVersion_StoresChecksumAndMakesItCurrent()
    {
        var world = new World();
        var file = await CreateFile(world);

        await using var content = new MemoryStream("%PDF-1.7 test content"u8.ToArray());
        var result = await world.UploadHandler().Handle(
            new UploadDocumentFileVersionCommand(
                file.FileId,
                world.Clerk.UserId,
                content,
                "scan.pdf",
                "application/pdf",
                content.Length,
                "النسخة الأولى"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        Assert.Equal(1, result.Value.CurrentVersionNumber);
        var version = Assert.Single(result.Value.Versions);
        Assert.True(version.IsCurrent);
        Assert.Equal(world.Storage.Checksum, version.ChecksumSha256);
        Assert.Equal("scan.pdf", version.OriginalFileName);
        Assert.Single(world.Storage.Saved);
        Assert.Equal(2, world.UnitOfWork.SaveChangesCallCount);
    }

    [Fact]
    public async Task UploadVersion_RejectsAnInvalidSignature()
    {
        var world = new World();
        var file = await CreateFile(world);
        world.Signatures.Failure = DomainErrors.Attachments.SignatureMismatch;

        await using var content = new MemoryStream("not a pdf"u8.ToArray());
        var result = await world.UploadHandler().Handle(
            new UploadDocumentFileVersionCommand(
                file.FileId, world.Clerk.UserId, content, "scan.pdf", "application/pdf", content.Length),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Attachments.SignatureMismatch.Code, result.Error.Code);
        Assert.Empty(world.Storage.Saved);
        Assert.Equal(0, world.Files.Files[0].CurrentVersionNumber);
    }

    [Fact]
    public async Task UploadVersion_ToAnArchivedFile_IsRefused()
    {
        var world = new World();
        var file = await CreateFile(world);
        var archive = await world.ArchiveHandler().Handle(
            new ArchiveDocumentFileCommand(file.FileId, world.Clerk.UserId), CancellationToken.None);
        Assert.True(archive.IsSuccess);

        await using var content = new MemoryStream("%PDF-1.7 test content"u8.ToArray());
        var result = await world.UploadHandler().Handle(
            new UploadDocumentFileVersionCommand(
                file.FileId, world.Clerk.UserId, content, "scan.pdf", "application/pdf", content.Length),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Attachments.FileArchived.Code, result.Error.Code);
    }

    [Fact]
    public async Task Download_ReturnsTheCurrentVersionBytes()
    {
        var world = new World();
        var file = await CreateFile(world);
        var bytes = "%PDF-1.7 test content"u8.ToArray();

        await using var content = new MemoryStream(bytes);
        await world.UploadHandler().Handle(
            new UploadDocumentFileVersionCommand(
                file.FileId, world.Clerk.UserId, content, "scan.pdf", "application/pdf", content.Length),
            CancellationToken.None);

        var result = await world.DownloadHandler().Handle(
            new DownloadDocumentFileVersionQuery(file.FileId, world.Clerk.UserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Code);
        await using var downloaded = result.Value.Content;
        await using var buffer = new MemoryStream();
        await downloaded.CopyToAsync(buffer);
        Assert.Equal(bytes, buffer.ToArray());
        Assert.Equal("scan.pdf", result.Value.OriginalFileName);
    }

    [Fact]
    public async Task Download_WithoutDownloadAccess_IsForbidden()
    {
        var world = new World();
        var file = await CreateFile(world);
        world.Access.ManagedDocuments.Clear();
        world.Access.ViewableDocuments.Add(world.Document.DocumentId);

        var result = await world.DownloadHandler().Handle(
            new DownloadDocumentFileVersionQuery(file.FileId, world.Clerk.UserId),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrors.Correspondence.DocumentAccessDenied.Code, result.Error.Code);
    }

    [Fact]
    public async Task Verify_ComparesStoredAndRecomputedChecksums()
    {
        var world = new World();
        var file = await CreateFile(world);
        var bytes = "%PDF-1.7 test content"u8.ToArray();

        await using var content = new MemoryStream(bytes);
        var upload = await world.UploadHandler().Handle(
            new UploadDocumentFileVersionCommand(
                file.FileId, world.Clerk.UserId, content, "scan.pdf", "application/pdf", content.Length),
            CancellationToken.None);

        // The fake storage returns the configured checksum from SaveAsync, so align it with the bytes.
        var versionId = upload.Value.Versions.Single().VersionId;
        var expected = await new FakeFileStorageService().ComputeSha256Async(new MemoryStream(bytes));
        var verify = await world.VerifyHandler().Handle(
            new VerifyAttachmentChecksumQuery(file.FileId, world.Clerk.UserId, versionId),
            CancellationToken.None);

        Assert.True(verify.IsSuccess, verify.Error?.Code);
        Assert.Equal(expected, verify.Value.ComputedChecksumSha256);
        Assert.False(verify.Value.IsMatch);
    }

    [Fact]
    public async Task List_ReturnsFilesForTheDocument()
    {
        var world = new World();
        await CreateFile(world);

        var result = await world.ListHandler().Handle(
            new ListDocumentFilesQuery(world.Document.DocumentId, world.Clerk.UserId),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value);
    }
}
