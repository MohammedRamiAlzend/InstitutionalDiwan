using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Drive;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Module 11 (Drive) — file binaries: presigned direct-to-storage uploads (initiate/complete/abandon),
/// streaming download with HTTP Range for video, short-lived presigned download URLs, the immutable
/// version list, per-version content, and copy-restore of an old version.
/// </summary>
[ApiController]
[Route("api/drive/files")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DriveUse)]
public sealed class DriveFilesController : ControllerBase
{
    private readonly ISender _sender;

    public DriveFilesController(ISender sender) => _sender = sender;

    /// <summary>Reserves a pending version and returns a presigned PUT URL for direct browser upload.</summary>
    /// <param name="request">The destination space/parent, file name, declared size, optional MIME type, and optional existing file id (to add a version).</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The pending version id, object key, presigned upload URL, and expiry.</response>
    /// <response code="400">The payload failed validation, the extension is disallowed, or the file is too large.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Editor access to the destination.</response>
    /// <response code="404">The space, parent, or file does not exist.</response>
    /// <response code="409">The space quota would be exceeded.</response>
    /// <response code="500">Object storage is not configured.</response>
    [HttpPost("initiate")]
    [EndpointSummary("Reserves a pending version and returns a presigned PUT URL for direct browser upload.")]
    [EndpointDescription("Pre-checks the extension, declared size, and remaining quota, reserves a Pending version, and signs a 15-minute presigned PUT URL so the browser uploads bytes directly to SeaweedFS. Supply fileItemId to add a version to an existing file. Requires the DRIVE_USE permission and Editor access to the destination. Responses: 200 (The pending version id, object key, presigned upload URL, and expiry.), 400 (The payload failed validation, the extension is disallowed, or the file is too large.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Editor access to the destination.), 404 (The space, parent, or file does not exist.), 409 (The space quota would be exceeded.), 500 (Object storage is not configured.)")]
    [ProducesResponseType<DriveUploadInitiatedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Initiate(
        [FromBody] InitiateDriveUploadRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new InitiateDriveUploadCommand(
            request.SpaceId, request.ParentId, request.FileName, request.DeclaredSizeBytes,
            request.MimeType, request.FileItemId), cancellationToken)).ToActionResult();

    /// <summary>Verifies the stored object and commits the pending version.</summary>
    /// <param name="versionId">Pending version unique identifier (GUID, route). Example: <c>6d4e8f10-3b72-4a95-8c61-9e05f7a2b403</c>.</param>
    /// <param name="request">Optional client SHA-256 checksum to verify against the stored object.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The committed version.</response>
    /// <response code="400">The stored size mismatches, the magic bytes fail, or the checksum mismatches.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Editor access.</response>
    /// <response code="404">The version or its stored object does not exist.</response>
    /// <response code="409">The version is not pending, or the quota would be exceeded.</response>
    [HttpPost("{versionId:guid}/complete")]
    [EndpointSummary("Verifies the stored object and commits the pending version.")]
    [EndpointDescription("After the browser PUTs the bytes, this checks the stored object (HEAD size, magic bytes, streamed SHA-256, optional client checksum), then commits the version and updates the space usage and current version atomically. Requires the DRIVE_USE permission and Editor access. Responses: 200 (The committed version.), 400 (The stored size mismatches, the magic bytes fail, or the checksum mismatches.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Editor access.), 404 (The version or its stored object does not exist.), 409 (The version is not pending, or the quota would be exceeded.)")]
    [ProducesResponseType<DriveVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Complete(
        [Description("Pending version unique identifier (GUID, route). Example: 6d4e8f10-3b72-4a95-8c61-9e05f7a2b403.")] Guid versionId,
        [FromBody] CompleteDriveUploadRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new CompleteDriveUploadCommand(versionId, request.ChecksumSha256), cancellationToken)).ToActionResult();

    /// <summary>Abandons a pending upload and deletes its half-uploaded object.</summary>
    /// <param name="versionId">Pending version unique identifier (GUID, route). Example: <c>6d4e8f10-3b72-4a95-8c61-9e05f7a2b403</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The upload is abandoned.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Editor access.</response>
    /// <response code="404">No version with that id exists.</response>
    /// <response code="409">The version is not pending.</response>
    [HttpDelete("{versionId:guid}/abandon")]
    [EndpointSummary("Abandons a pending upload and deletes its half-uploaded object.")]
    [EndpointDescription("Marks a Pending version abandoned and best-effort deletes its object. Requires the DRIVE_USE permission and Editor access. Responses: 200 (The upload is abandoned.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Editor access.), 404 (No version with that id exists.), 409 (The version is not pending.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Abandon(
        [Description("Pending version unique identifier (GUID, route). Example: 6d4e8f10-3b72-4a95-8c61-9e05f7a2b403.")] Guid versionId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new AbandonDriveUploadCommand(versionId), cancellationToken)).ToActionResult();

    /// <summary>Streams the current (or a named) version's bytes; supports HTTP Range for video.</summary>
    /// <param name="itemId">File item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="versionId">Version id (GUID, query, optional); defaults to the current version. Example: <c>6d4e8f10-3b72-4a95-8c61-9e05f7a2b403</c>.</param>
    /// <param name="disposition">Content disposition (string, query, optional; inline/attachment). Example: <c>inline</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The full file bytes.</response>
    /// <response code="206">The requested byte range (Content-Range header).</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Viewer access.</response>
    /// <response code="404">The file, version, or stored object does not exist.</response>
    [HttpGet("{itemId:guid}/content")]
    [EndpointSummary("Streams the current (or a named) version's bytes; supports HTTP Range for video.")]
    [EndpointDescription("Streams a file version's bytes. A bytes Range request header yields 206 with Content-Range; otherwise the full object is returned with 200. disposition=attachment forces a download. Requires the DRIVE_USE permission and Viewer access. Responses: 200 (The full file bytes.), 206 (The requested byte range.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Viewer access.), 404 (The file, version, or stored object does not exist.)")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status206PartialContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Content(
        [Description("File item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        [Description("Version id (GUID, query, optional); defaults to the current version. Example: 6d4e8f10-3b72-4a95-8c61-9e05f7a2b403.")] Guid? versionId,
        [Description("Content disposition (string, query, optional; inline/attachment). Example: inline.")] string? disposition,
        CancellationToken cancellationToken)
        => StreamContentAsync(itemId, versionId, disposition, cancellationToken);

    /// <summary>Signs a short-lived presigned download URL for the browser or OnlyOffice.</summary>
    /// <param name="itemId">File item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="versionId">Version id (GUID, query, optional); defaults to the current version. Example: <c>6d4e8f10-3b72-4a95-8c61-9e05f7a2b403</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The presigned URL and its expiry.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Viewer access.</response>
    /// <response code="404">The file or version does not exist.</response>
    /// <response code="500">Object storage is not configured.</response>
    [HttpGet("{itemId:guid}/download-url")]
    [EndpointSummary("Signs a short-lived presigned download URL for the browser or OnlyOffice.")]
    [EndpointDescription("Returns a 5-minute presigned GET URL the browser (or the OnlyOffice server) can fetch directly from SeaweedFS. Requires the DRIVE_USE permission and Viewer access. Responses: 200 (The presigned URL and its expiry.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Viewer access.), 404 (The file or version does not exist.), 500 (Object storage is not configured.)")]
    [ProducesResponseType<DriveDownloadUrlResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DownloadUrl(
        [Description("File item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        [Description("Version id (GUID, query, optional); defaults to the current version. Example: 6d4e8f10-3b72-4a95-8c61-9e05f7a2b403.")] Guid? versionId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new GetDriveDownloadUrlQuery(itemId, versionId), cancellationToken)).ToActionResult();

    /// <summary>Lists a file's immutable versions, newest first.</summary>
    /// <param name="itemId">File item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The version history, newest first.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Viewer access.</response>
    /// <response code="404">No file with that id exists.</response>
    [HttpGet("{itemId:guid}/versions")]
    [EndpointSummary("Lists a file's immutable versions, newest first.")]
    [EndpointDescription("Returns every committed version of a file, newest first, marking the current one. Requires the DRIVE_USE permission and Viewer access. Responses: 200 (The version history, newest first.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Viewer access.), 404 (No file with that id exists.)")]
    [ProducesResponseType<IReadOnlyList<DriveVersionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Versions(
        [Description("File item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new ListDriveFileVersionsQuery(itemId), cancellationToken)).ToActionResult();

    /// <summary>Streams a specific version's bytes; supports HTTP Range.</summary>
    /// <param name="itemId">File item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="versionId">Version unique identifier (GUID, route). Example: <c>6d4e8f10-3b72-4a95-8c61-9e05f7a2b403</c>.</param>
    /// <param name="disposition">Content disposition (string, query, optional; inline/attachment). Example: <c>attachment</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The full version bytes.</response>
    /// <response code="206">The requested byte range.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Viewer access.</response>
    /// <response code="404">The file, version, or stored object does not exist.</response>
    [HttpGet("{itemId:guid}/versions/{versionId:guid}/content")]
    [EndpointSummary("Streams a specific version's bytes; supports HTTP Range.")]
    [EndpointDescription("Streams the bytes of one named version (not necessarily the current one), with HTTP Range support. Requires the DRIVE_USE permission and Viewer access. Responses: 200 (The full version bytes.), 206 (The requested byte range.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Viewer access.), 404 (The file, version, or stored object does not exist.)")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status206PartialContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public Task<IActionResult> VersionContent(
        [Description("File item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        [Description("Version unique identifier (GUID, route). Example: 6d4e8f10-3b72-4a95-8c61-9e05f7a2b403.")] Guid versionId,
        [Description("Content disposition (string, query, optional; inline/attachment). Example: attachment.")] string? disposition,
        CancellationToken cancellationToken)
        => StreamContentAsync(itemId, versionId, disposition, cancellationToken);

    /// <summary>Restores an old version by copying it into a new current version.</summary>
    /// <param name="itemId">File item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="versionId">Version unique identifier to restore (GUID, route). Example: <c>6d4e8f10-3b72-4a95-8c61-9e05f7a2b403</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The new version created from the restored copy.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Editor access.</response>
    /// <response code="404">The file or version does not exist.</response>
    /// <response code="409">The space quota would be exceeded.</response>
    /// <response code="500">Object storage is not configured.</response>
    [HttpPost("{itemId:guid}/versions/{versionId:guid}/restore")]
    [EndpointSummary("Restores an old version by copying it into a new current version.")]
    [EndpointDescription("Restores a previous version by server-side copying its object into a brand-new version (Source=Restore) and making it current; history is never edited. Requires the DRIVE_USE permission and Editor access. Responses: 200 (The new version created from the restored copy.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Editor access.), 404 (The file or version does not exist.), 409 (The space quota would be exceeded.), 500 (Object storage is not configured.)")]
    [ProducesResponseType<DriveVersionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RestoreVersion(
        [Description("File item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        [Description("Version unique identifier to restore (GUID, route). Example: 6d4e8f10-3b72-4a95-8c61-9e05f7a2b403.")] Guid versionId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new RestoreDriveFileVersionCommand(itemId, versionId), cancellationToken)).ToActionResult();

    /// <summary>Streams a version, honoring a bytes Range header with a 206 + Content-Range response.</summary>
    private async Task<IActionResult> StreamContentAsync(Guid itemId, Guid? versionId, string? disposition, CancellationToken cancellationToken)
    {
        long? rangeStart = null;
        long? rangeEnd = null;
        var range = Request.GetTypedHeaders().Range;
        if (range is not null &&
            string.Equals(range.Unit.Value, "bytes", StringComparison.OrdinalIgnoreCase) &&
            range.Ranges.FirstOrDefault() is { From: { } from } first)
        {
            rangeStart = from;
            rangeEnd = first.To;
        }

        var result = await _sender.Send(new GetDriveFileContentQuery(itemId, versionId, rangeStart, rangeEnd), cancellationToken);
        if (result.IsFailure)
            return result.Error.ToActionResult();

        var content = result.Value;
        Response.Headers.AcceptRanges = "bytes";
        var dispositionType = string.Equals(disposition, "attachment", StringComparison.OrdinalIgnoreCase)
            ? "attachment"
            : "inline";
        Response.Headers.ContentDisposition = new ContentDispositionHeaderValue(dispositionType)
        {
            FileNameStar = content.FileName
        }.ToString();

        if (content.RangeStart is { } start)
        {
            Response.StatusCode = StatusCodes.Status206PartialContent;
            Response.Headers.ContentRange = $"bytes {start}-{content.RangeEnd}/{content.TotalSizeBytes}";
            return File(content.Content, content.MimeType);
        }

        return File(content.Content, content.MimeType);
    }
}