using InstitutionalDiwan.Application.Features.Attachments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Server.Contracts.Attachments;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Module 06 — document files and versioned storage. Every file keeps an immutable, sequential
/// chain of versions; each version records a 64-character SHA-256 checksum computed while the
/// upload streamed to storage. Uploads are rejected unless the extension, size, and magic bytes
/// match the selected file type.
/// </summary>
[ApiController]
[Route("api/document-files")]
[Produces("application/json")]
[Authorize]
public sealed class DocumentFilesController : ControllerBase
{
    /// <summary>Upper bound that matches the largest seeded file type (50 MB), plus multipart overhead.</summary>
    private const long MaxUploadBytes = 52_428_800 + 1_048_576;

    private readonly ISender _sender;

    public DocumentFilesController(ISender sender) => _sender = sender;

    /// <summary>Opens a new document file (version chain) with its metadata.</summary>
    /// <remarks>
    /// This creates the file record only; upload the first version with
    /// <c>POST /api/document-files/{fileId}/versions</c>. The document must exist and the caller must
    /// hold MANAGE access to it (a document's author always does).
    /// </remarks>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>documentId</term><description>Document id (GUID, required). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</description></item>
    /// <item><term>fileTypeId</term><description>File type id (integer, required; 1 = scan, 2 = attachment, 3 = signature, 4 = other). Example: <c>1</c>.</description></item>
    /// <item><term>title</term><description>Display title (string, required, max 300). Example: <c>النسخة الممسوحة ضوئياً</c>.</description></item>
    /// <item><term>isPrimaryScan</term><description>Whether this is the document's primary scan (boolean, optional, default <c>false</c>). Example: <c>true</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created file with an empty version list.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_EDIT, or the caller cannot manage the document.</response>
    /// <response code="404">The document or file type does not exist.</response>
    /// <response code="409">The file type is inactive.</response>
    [HttpPost]
    [EndpointSummary("Opens a new document file (version chain) with its metadata.")]
    [EndpointDescription("Opens a new document file (version chain) with its metadata. The bytes are uploaded separately to POST /api/document-files/{fileId}/versions. Requires the DOCUMENT_EDIT permission and MANAGE access to the document. Responses: 200 (The created file with an empty version list.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_EDIT, or the caller cannot manage the document.), 404 (The document or file type does not exist.), 409 (The file type is inactive.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentFileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDocumentFileRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new CreateDocumentFileCommand(
                request.DocumentId,
                request.FileTypeId,
                request.Title,
                userId,
                request.IsPrimaryScan),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Creates a document file and uploads its first version in one request.</summary>
    /// <remarks>
    /// This is the planned Module 06 entry point: it validates the metadata and the binary in the
    /// same transaction, so a document file never exists without its first version.
    /// </remarks>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="file">The binary upload (multipart/form-data, required). Example: <c>scan.pdf</c>.</param>
    /// <param name="fileTypeId">File type id (integer, form, required; 1 = scan, 2 = attachment). Example: <c>1</c>.</param>
    /// <param name="title">Display title (string, form, required, max 300). Example: <c>النسخة الممسوحة ضوئياً</c>.</param>
    /// <param name="isPrimaryScan">Whether this is the document's primary scan (boolean, form, optional, default <c>false</c>). Example: <c>true</c>.</param>
    /// <param name="changeNotes">Optional notes describing the first version (string, form, optional, max 500). Example: <c>Initial scan.</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created file with version 1.</response>
    /// <response code="400">The payload failed validation, or the upload is empty, too large, has a disallowed extension, or fails the magic-byte check.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_EDIT, or the caller cannot manage the document.</response>
    /// <response code="404">The document, file type, or storage provider does not exist.</response>
    /// <response code="409">The file type is inactive.</response>
    [HttpPost("/api/documents/{documentId:guid}/files")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    [Consumes("multipart/form-data")]
    [EndpointSummary("Creates a document file and uploads its first version in one request.")]
    [EndpointDescription("Creates a document file and stores version 1 atomically: validates the metadata, extension, size, and magic bytes, computes the SHA-256 checksum while streaming to storage, and returns the file with its first version. Requires the DOCUMENT_EDIT permission and MANAGE access to the document. Responses: 200 (The created file with version 1.), 400 (The payload failed validation, or the upload is empty, too large, has a disallowed extension, or fails the magic-byte check.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_EDIT, or the caller cannot manage the document.), 404 (The document, file type, or storage provider does not exist.), 409 (The file type is inactive.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentFileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateWithVersion(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        [FromForm, Description("The binary upload (multipart/form-data). Example: scan.pdf.")] IFormFile file,
        [FromForm, Description("File type id (integer, form). Example: 1.")] int fileTypeId,
        [FromForm, Description("Display title (string, form). Example: النسخة الممسوحة ضوئياً.")] string title,
        [FromForm, Description("Whether this is the primary scan (boolean, form). Example: true.")] bool isPrimaryScan,
        [FromForm, Description("Optional version notes (string, form). Example: Initial scan.")] string? changeNotes,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        if (file is null || file.Length == 0)
            return DomainErrors.Attachments.FileEmpty.ToActionResult();

        await using var content = file.OpenReadStream();
        var result = await _sender.Send(
            new CreateDocumentFileWithVersionCommand(
                documentId,
                fileTypeId,
                title,
                userId,
                content,
                file.FileName,
                file.ContentType,
                file.Length,
                isPrimaryScan,
                changeNotes),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Uploads the next immutable version of a document file.</summary>
    /// <remarks>
    /// The upload is validated before anything is persisted: the extension must be allowed for the
    /// file type, the size must be within its limit, and the magic bytes must match the real format.
    /// The SHA-256 checksum is computed while the stream is written to storage and stored on the
    /// version. An archived file cannot receive new versions.
    /// </remarks>
    /// <param name="fileId">Document file unique identifier (GUID, route). Example: <c>2b7c1e94-5d38-4a6f-9c21-8e04b7d63f15</c>.</param>
    /// <param name="file">The binary upload (multipart/form-data, required). Example: <c>scan.pdf</c>.</param>
    /// <param name="changeNotes">Optional notes describing this version (string, form, optional, max 500). Example: <c>Re-scan with higher resolution.</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The file with its new current version.</response>
    /// <response code="400">The upload is empty, too large, has a disallowed extension, or fails the magic-byte check.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_EDIT, or the caller cannot manage the document.</response>
    /// <response code="404">The file, its file type, or the storage provider does not exist.</response>
    /// <response code="409">The file is archived or its file type is inactive.</response>
    [HttpPost("{fileId:guid}/versions")]
    [RequestSizeLimit(MaxUploadBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
    [Consumes("multipart/form-data")]
    [EndpointSummary("Uploads the next immutable version of a document file.")]
    [EndpointDescription("Uploads the next immutable version of a document file. Validates the extension, size, and magic bytes, computes the SHA-256 checksum while streaming to storage, and refuses an archived file. Requires the DOCUMENT_EDIT permission and MANAGE access to the document. Responses: 200 (The file with its new current version.), 400 (The upload is empty, too large, has a disallowed extension, or fails the magic-byte check.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_EDIT, or the caller cannot manage the document.), 404 (The file, its file type, or the storage provider does not exist.), 409 (The file is archived or its file type is inactive.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentFileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UploadVersion(
        [Description("Document file unique identifier (GUID, route). Example: 2b7c1e94-5d38-4a6f-9c21-8e04b7d63f15.")] Guid fileId,
        [FromForm, Description("The binary upload (multipart/form-data). Example: scan.pdf.")] IFormFile file,
        [FromForm, Description("Optional notes describing this version (max 500). Example: Re-scan with higher resolution.")] string? changeNotes,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        if (file is null || file.Length == 0)
            return DomainErrors.Attachments.FileEmpty.ToActionResult();

        await using var content = file.OpenReadStream();
        var result = await _sender.Send(
            new UploadDocumentFileVersionCommand(
                fileId,
                userId,
                content,
                file.FileName,
                file.ContentType,
                file.Length,
                changeNotes),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Archives a document file so no further versions can be uploaded.</summary>
    /// <param name="fileId">Document file unique identifier (GUID, route). Example: <c>2b7c1e94-5d38-4a6f-9c21-8e04b7d63f15</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The archived file; its versions stay readable.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_EDIT, or the caller cannot manage the document.</response>
    /// <response code="404">No file with that id exists.</response>
    [HttpPost("{fileId:guid}/archive")]
    [EndpointSummary("Archives a document file so no further versions can be uploaded.")]
    [EndpointDescription("Archives a document file so no further versions can be uploaded; its versions stay readable. Requires the DOCUMENT_EDIT permission and MANAGE access to the document. Responses: 200 (The archived file; its versions stay readable.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_EDIT, or the caller cannot manage the document.), 404 (No file with that id exists.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<DocumentFileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(
        [Description("Document file unique identifier (GUID, route). Example: 2b7c1e94-5d38-4a6f-9c21-8e04b7d63f15.")] Guid fileId,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new ArchiveDocumentFileCommand(fileId, userId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Lists a document's files and every version each one has held.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The document's files, oldest first.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_VIEW, or the caller cannot view the document.</response>
    /// <response code="404">The caller's user account was not found.</response>
    [HttpGet("by-document/{documentId:guid}")]
    [EndpointSummary("Lists a document's files and every version each one has held.")]
    [EndpointDescription("Lists a document's files, oldest first, each with its full version history. Requires the DOCUMENT_VIEW permission and VIEW access to the document. Responses: 200 (The document's files, oldest first.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_VIEW, or the caller cannot view the document.), 404 (The caller's user account was not found.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<IReadOnlyCollection<DocumentFileResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListByDocument(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new ListDocumentFilesQuery(documentId, userId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Lists a document's files using the planned document-scoped route.</summary>
    /// <param name="documentId">Document unique identifier (GUID, route). Example: <c>7c9e3b42-8a61-4d52-9f17-2ab85c6e7934</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The document's files, oldest first.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_VIEW, or the caller cannot view the document.</response>
    /// <response code="404">The caller's user account was not found.</response>
    [HttpGet("/api/documents/{documentId:guid}/files")]
    [EndpointSummary("Lists a document's files using the planned document-scoped route.")]
    [EndpointDescription("Lists a document's files, oldest first, each with its full version history. This is the planned document-scoped alias of GET /api/document-files/by-document/{documentId}. Requires the DOCUMENT_VIEW permission and VIEW access to the document. Responses: 200 (The document's files, oldest first.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_VIEW, or the caller cannot view the document.), 404 (The caller's user account was not found.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<IReadOnlyCollection<DocumentFileResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ListByDocumentScoped(
        [Description("Document unique identifier (GUID, route). Example: 7c9e3b42-8a61-4d52-9f17-2ab85c6e7934.")] Guid documentId,
        CancellationToken cancellationToken)
        => ListByDocument(documentId, cancellationToken);

    /// <summary>Downloads one version of a document file; omit the version for the current one.</summary>
    /// <param name="fileId">Document file unique identifier (GUID, route). Example: <c>2b7c1e94-5d38-4a6f-9c21-8e04b7d63f15</c>.</param>
    /// <param name="versionId">File version unique identifier (GUID, query, optional). Defaults to the current version. Example: <c>8f1c2d43-6a09-4e75-b3d8-5c17f90e2a64</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The file bytes with their original name and MIME type.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_VIEW, or the caller cannot download the document.</response>
    /// <response code="404">The file, version, provider, or stored bytes were not found.</response>
    [HttpGet("{fileId:guid}/download")]
    [EndpointSummary("Downloads one version of a document file; omit the version for the current one.")]
    [EndpointDescription("Streams the bytes of one document file version, defaulting to the current version. Requires the DOCUMENT_VIEW permission and DOWNLOAD access to the document. Responses: 200 (The file bytes with their original name and MIME type.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_VIEW, or the caller cannot download the document.), 404 (The file, version, provider, or stored bytes were not found.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(
        [Description("Document file unique identifier (GUID, route). Example: 2b7c1e94-5d38-4a6f-9c21-8e04b7d63f15.")] Guid fileId,
        [FromQuery, Description("File version unique identifier (GUID, optional). Defaults to the current version. Example: 8f1c2d43-6a09-4e75-b3d8-5c17f90e2a64.")] Guid? versionId,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new DownloadDocumentFileVersionQuery(fileId, userId, versionId), cancellationToken);

        if (result.IsFailure)
            return result.Error.ToActionResult();

        return File(
            result.Value.Content,
            result.Value.MimeType,
            result.Value.OriginalFileName,
            enableRangeProcessing: true);
    }

    /// <summary>Recomputes a version's SHA-256 and compares it with the stored checksum.</summary>
    /// <param name="fileId">Document file unique identifier (GUID, route). Example: <c>2b7c1e94-5d38-4a6f-9c21-8e04b7d63f15</c>.</param>
    /// <param name="versionId">File version unique identifier (GUID, query, optional). Defaults to the current version. Example: <c>8f1c2d43-6a09-4e75-b3d8-5c17f90e2a64</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The recorded and recomputed checksums, plus <c>isMatch</c>.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DOCUMENT_VIEW, or the caller cannot view the document.</response>
    /// <response code="404">The file, version, provider, or stored bytes were not found.</response>
    [HttpGet("{fileId:guid}/verify")]
    [EndpointSummary("Recomputes a version's SHA-256 and compares it with the stored checksum.")]
    [EndpointDescription("Re-reads one file version from storage, recomputes its SHA-256, and compares it with the checksum recorded at upload time. Omit the version to verify the current one. Requires the DOCUMENT_VIEW permission and VIEW access to the document. Responses: 200 (The recorded and recomputed checksums, plus isMatch.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DOCUMENT_VIEW, or the caller cannot view the document.), 404 (The file, version, provider, or stored bytes were not found.)")]
    [Authorize(Policy = Permissions.DocumentView)]
    [ProducesResponseType<AttachmentChecksumResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Verify(
        [Description("Document file unique identifier (GUID, route). Example: 2b7c1e94-5d38-4a6f-9c21-8e04b7d63f15.")] Guid fileId,
        [FromQuery, Description("File version unique identifier (GUID, optional). Defaults to the current version. Example: 8f1c2d43-6a09-4e75-b3d8-5c17f90e2a64.")] Guid? versionId,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new VerifyAttachmentChecksumQuery(fileId, userId, versionId), cancellationToken);

        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
