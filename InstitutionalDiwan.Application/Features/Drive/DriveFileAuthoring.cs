using System.IO.Compression;
using System.Text;
using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Authorization;
using InstitutionalDiwan.Application.Abstractions.Messaging;
using InstitutionalDiwan.Application.Abstractions.Storage;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Attachments;
using InstitutionalDiwan.Domain.Entities.Drive;
using InstitutionalDiwan.Domain.Entities.Organization;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;

namespace InstitutionalDiwan.Application.Features.Drive;

/// <summary>
/// Creates an empty Word (.docx), Excel (.xlsx), plain-text (.txt) or Markdown (.md) file with a
/// committed first version, so the caller can open it in the editor straight away. Editor access to
/// the destination is required, exactly like an upload.
/// </summary>
public sealed record CreateDriveFileCommand(Guid SpaceId, Guid? ParentId, string FileName)
    : ICommand<DriveItemResponse>;

/// <summary>
/// Replaces the content of a .txt or .md file and stores it as a new immutable version (the previous
/// versions stay intact). Requires Editor access; an unchanged save creates no duplicate version.
/// </summary>
public sealed record SaveDriveTextContentCommand(Guid ItemId, string Content)
    : ICommand<DriveVersionResponse>;

/// <summary>The empty-document templates and content types offered by the create-file command.</summary>
internal static class DriveNewFileTemplates
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ["xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ["txt"] = "text/plain",
        ["md"] = "text/markdown"
    };

    /// <summary>Extensions a new file may be created with, without the leading dot.</summary>
    public static IReadOnlyCollection<string> Extensions => ContentTypes.Keys;

    public static string ExtensionOf(string fileName) =>
        Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();

    public static bool IsSupported(string fileName) => ContentTypes.ContainsKey(ExtensionOf(fileName));

    /// <summary>True for the plain-text formats edited by the in-app text editor instead of OnlyOffice.</summary>
    public static bool IsText(string fileName) =>
        ExtensionOf(fileName) is "txt" or "md";

    /// <summary>
    /// The first version's bytes and MIME type. A new text file starts with a newline so it already
    /// has content (versions must be non-empty); Office files get a minimal, valid OOXML package.
    /// </summary>
    public static (byte[] Content, string MimeType)? Create(string fileName)
    {
        var extension = ExtensionOf(fileName);
        if (!ContentTypes.TryGetValue(extension, out var mimeType))
            return null;

        var content = extension switch
        {
            "docx" => BuildDocx(),
            "xlsx" => BuildXlsx(),
            _ => Encoding.UTF8.GetBytes("\n")
        };
        return (content, mimeType);
    }

    private static byte[] BuildZip(params (string Path, string Xml)[] parts)
    {
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, xml) in parts)
            {
                var entry = archive.CreateEntry(path, CompressionLevel.Optimal);
                using var stream = entry.Open();
                using var writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.Write(xml);
            }
        }
        return buffer.ToArray();
    }

    private static byte[] BuildDocx() => BuildZip(
        ("[Content_Types].xml",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/></Types>
            """),
        ("_rels/.rels",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/></Relationships>
            """),
        ("word/document.xml",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body><w:p/></w:body></w:document>
            """));

    private static byte[] BuildXlsx() => BuildZip(
        ("[Content_Types].xml",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/></Types>
            """),
        ("_rels/.rels",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>
            """),
        ("xl/workbook.xml",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="Sheet1" sheetId="1" r:id="rId1"/></sheets></workbook>
            """),
        ("xl/_rels/workbook.xml.rels",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/></Relationships>
            """),
        ("xl/worksheets/sheet1.xml",
            """
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><sheetData/></worksheet>
            """));
}

internal sealed class CreateDriveFileCommandHandler
    : ICommandHandler<CreateDriveFileCommand, DriveItemResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveSpaceRepository _spaces;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveStorageService _storage;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public CreateDriveFileCommandHandler(
        IDriveItemRepository items,
        IDriveSpaceRepository spaces,
        IDriveFileVersionRepository versions,
        IInstitutionSettingRepository settings,
        IDriveStorageService storage,
        IDriveAccessEvaluator access,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _spaces = spaces;
        _versions = versions;
        _settings = settings;
        _storage = storage;
        _access = access;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveItemResponse>> Handle(
        CreateDriveFileCommand request, CancellationToken cancellationToken)
    {
        var setting = await _settings.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.StorageNotConfigured);

        if (!DriveNewFileTemplates.IsSupported(request.FileName) ||
            !DriveUploadGuard.IsExtensionAllowed(
                Path.GetExtension(request.FileName), setting.DriveAllowedExtensionsCsv))
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.FileTypeNotCreatable);

        var template = DriveNewFileTemplates.Create(request.FileName);
        if (template is null)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.FileTypeNotCreatable);
        var (content, mimeType) = template.Value;

        var space = await _spaces.GetByIdAsync(request.SpaceId, cancellationToken);
        if (space is null || space.IsTrashed)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.SpaceNotFound);

        var depth = 1;
        if (request.ParentId is { } parentId)
        {
            var parent = await _items.GetByIdAsync(parentId, cancellationToken);
            if (parent is null || parent.SpaceId != request.SpaceId)
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.ItemNotFound);
            if (!parent.IsFolder)
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.NotAFolder);
            if (!await _access.CanAccessItemAsync(parentId, DriveRole.EditorId, cancellationToken))
                return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);
            depth = await _items.GetDepthAsync(parentId, cancellationToken) + 1;
        }
        else if (space.OwnerUserId != _userContext.UserId && !_access.IsAdmin)
        {
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.AccessDenied);
        }

        if (await _items.NameExistsAsync(
                request.SpaceId, request.ParentId, request.FileName.Trim(), null, cancellationToken))
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.NameTaken);

        var created = DriveItem.CreateFile(
            request.SpaceId, request.ParentId, request.FileName, _userContext.UserId, depth, _unitOfWork.UtcNow);
        if (created.IsFailure)
            return Result.Failure<DriveItemResponse>(created.Error);
        var file = created.Value;

        var size = content.LongLength;
        if (size > setting.DriveMaxFileSizeBytes)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.FileTooLarge);

        var effectiveQuota = CreateDriveSpaceCommandHandler.EffectiveQuota(space, setting);
        if (space.UsedBytes + size > effectiveQuota)
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.QuotaExceeded);

        var objectKey = $"{file.SpaceId}/{file.ItemId}/1";
        string checksum;
        try
        {
            await using var writeStream = new MemoryStream(content);
            await _storage.WriteAsync(objectKey, writeStream, mimeType, cancellationToken);
            await using var checksumStream = new MemoryStream(content);
            checksum = await _storage.ComputeSha256Async(checksumStream, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<DriveItemResponse>(DomainErrors.Drive.StorageNotConfigured);
        }

        var now = _unitOfWork.UtcNow;
        var pending = DriveFileVersion.CreatePending(
            file.ItemId, 1, FileStorageProvider.DriveSeaweedFsId, objectKey, size, mimeType,
            DriveVersionSource.UploadId, _userContext.UserId, now);
        if (pending.IsFailure)
            return Result.Failure<DriveItemResponse>(pending.Error);
        var version = pending.Value;

        version.Commit(checksum, size, now);
        space.AddUsage(size, effectiveQuota, now);
        file.SetCurrentVersion(version.VersionId, 1, size, checksum, now);
        _items.Add(file);
        _versions.Add(version);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(DriveItemResponse.From(file));
    }
}

internal sealed class SaveDriveTextContentCommandHandler
    : ICommandHandler<SaveDriveTextContentCommand, DriveVersionResponse>
{
    private readonly IDriveItemRepository _items;
    private readonly IDriveSpaceRepository _spaces;
    private readonly IDriveFileVersionRepository _versions;
    private readonly IInstitutionSettingRepository _settings;
    private readonly IDriveStorageService _storage;
    private readonly IDriveAccessEvaluator _access;
    private readonly IUserContext _userContext;
    private readonly IUnitOfWork _unitOfWork;

    public SaveDriveTextContentCommandHandler(
        IDriveItemRepository items,
        IDriveSpaceRepository spaces,
        IDriveFileVersionRepository versions,
        IInstitutionSettingRepository settings,
        IDriveStorageService storage,
        IDriveAccessEvaluator access,
        IUserContext userContext,
        IUnitOfWork unitOfWork)
    {
        _items = items;
        _spaces = spaces;
        _versions = versions;
        _settings = settings;
        _storage = storage;
        _access = access;
        _userContext = userContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<DriveVersionResponse>> Handle(
        SaveDriveTextContentCommand request, CancellationToken cancellationToken)
    {
        var file = await _items.GetByIdAsync(request.ItemId, cancellationToken);
        if (file is null)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.ItemNotFound);
        if (!file.IsFile)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.NotAFile);
        if (file.IsTrashed)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.ItemTrashed);
        if (!DriveNewFileTemplates.IsText(file.Name))
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.EditingNotSupported);
        if (!await _access.CanAccessItemAsync(file.ItemId, DriveRole.EditorId, cancellationToken))
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.AccessDenied);

        var setting = await _settings.GetSingletonAsync(cancellationToken);
        if (setting is null)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.StorageNotConfigured);

        // A version must be non-empty, so clearing the whole document stores a single newline.
        var text = string.IsNullOrEmpty(request.Content) ? "\n" : request.Content;
        var bytes = Encoding.UTF8.GetBytes(text);
        var size = (long)bytes.Length;
        if (size > setting.DriveMaxFileSizeBytes)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.FileTooLarge);

        var baseVersion = file.CurrentVersionId is null
            ? null
            : await _versions.GetByIdAsync(file.CurrentVersionId.Value, cancellationToken);
        var mimeType = DriveNewFileTemplates.ExtensionOf(file.Name) == "md"
            ? "text/markdown"
            : "text/plain";

        string checksum;
        await using (var checksumStream = new MemoryStream(bytes))
            checksum = await _storage.ComputeSha256Async(checksumStream, cancellationToken);

        // Dedup: saving an unchanged document must not add a version.
        if (baseVersion is not null &&
            string.Equals(checksum, baseVersion.ChecksumSha256, StringComparison.OrdinalIgnoreCase))
            return Result.Success(DriveVersionResponse.From(baseVersion, file.CurrentVersionId));

        var space = await _spaces.GetByIdAsync(file.SpaceId, cancellationToken);
        if (space is null)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.SpaceNotFound);
        var effectiveQuota = CreateDriveSpaceCommandHandler.EffectiveQuota(space, setting);
        if (space.UsedBytes + size > effectiveQuota)
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.QuotaExceeded);

        var now = _unitOfWork.UtcNow;
        var versionNumber = await _versions.GetNextVersionNumberAsync(file.ItemId, cancellationToken);
        var objectKey = $"{file.SpaceId}/{file.ItemId}/{versionNumber}";
        try
        {
            await using var writeStream = new MemoryStream(bytes);
            await _storage.WriteAsync(objectKey, writeStream, mimeType, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return Result.Failure<DriveVersionResponse>(DomainErrors.Drive.StorageNotConfigured);
        }

        var pending = DriveFileVersion.CreatePending(
            file.ItemId, versionNumber, FileStorageProvider.DriveSeaweedFsId, objectKey, size, mimeType,
            DriveVersionSource.EditorSaveId, _userContext.UserId, now);
        if (pending.IsFailure)
            return Result.Failure<DriveVersionResponse>(pending.Error);
        var version = pending.Value;

        version.Commit(checksum, size, now);
        space.AddUsage(size, effectiveQuota, now);
        file.SetCurrentVersion(version.VersionId, versionNumber, size, checksum, now);
        _versions.Add(version);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(DriveVersionResponse.From(version, version.VersionId));
    }
}
