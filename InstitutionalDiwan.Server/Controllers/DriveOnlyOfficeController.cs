using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Drive;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Module 11 (Drive) — OnlyOffice editing: the signed editor configuration for a file, and the
/// save callback OnlyOffice posts (authenticated by its own JWT, with no ambient user session).
/// </summary>
[ApiController]
[Route("api/drive")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DriveUse)]
public sealed class DriveOnlyOfficeController : ControllerBase
{
    private readonly ISender _sender;

    public DriveOnlyOfficeController(ISender sender) => _sender = sender;

    /// <summary>Builds the OnlyOffice editor config (and signed token) for a Drive file.</summary>
    /// <param name="itemId">File item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The editor server URL, document type, mode, session id, version, and full signed config.</response>
    /// <response code="400">The file type cannot be edited in OnlyOffice.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Viewer access.</response>
    /// <response code="404">The file or its current version does not exist.</response>
    /// <response code="500">OnlyOffice or object storage is not configured.</response>
    [HttpGet("files/{itemId:guid}/edit-config")]
    [EndpointSummary("Builds the OnlyOffice editor config (and signed token) for a Drive file.")]
    [EndpointDescription("Returns the config the browser passes to DocsAPI.DocEditor: a presigned document URL reachable by the OnlyOffice server, the unique document key, the callback URL, and an HS256 token. Editors get edit mode; Viewers get view-only. Opens or reuses an edit session. Requires the DRIVE_USE permission and Viewer access. Responses: 200 (The editor server URL, document type, mode, session id, version, and full signed config.), 400 (The file type cannot be edited in OnlyOffice.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Viewer access.), 404 (The file or its current version does not exist.), 500 (OnlyOffice or object storage is not configured.)")]
    [ProducesResponseType<DriveEditConfigResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> EditConfig(
        [Description("File item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new GetDriveEditConfigQuery(itemId), cancellationToken)).ToActionResult();

    /// <summary>Receives the OnlyOffice save callback; always answers HTTP 200 with an error code.</summary>
    /// <param name="request">The callback body (key, status, url, users, forcesavetype, token).</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The protocol answer: error=0 acknowledged, error=1 rejected.</response>
    [HttpPost("onlyoffice/callback")]
    [AllowAnonymous]
    [EndpointSummary("Receives the OnlyOffice save callback; always answers HTTP 200 with an error code.")]
    [EndpointDescription("OnlyOffice posts here when a document is edited, saved, forcesaved or closed. The HS256 callback token is verified (no ambient user session); a save downloads the edited bytes, rejects conflicts and duplicates, and commits a new version. Per the OnlyOffice protocol the HTTP status is always 200 and the body carries error=0 (success) or error=1 (failure).")]
    [ProducesResponseType<DriveOnlyOfficeCallbackResult>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Callback(
        [FromBody] DriveOnlyOfficeCallbackRequest request,
        CancellationToken cancellationToken)
    {
        var payload = new DriveOnlyOfficeCallbackPayload(
            request.Key, request.Status, request.Url, request.Users, request.ForceSaveType, request.Token);
        var result = await _sender.Send(new ProcessDriveOnlyOfficeCallbackCommand(payload), cancellationToken);
        var answer = result.IsSuccess ? result.Value : DriveOnlyOfficeCallbackResult.Failed;
        return Ok(answer);
    }
}