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
/// Module 11 (Drive) — spaces. Every user has one personal space «ملفاتي» (lazily ensured) and may
/// create shared spaces. Quota overrides are administrative; per-item access is checked in the handlers.
/// </summary>
[ApiController]
[Route("api/drive/spaces")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DriveUse)]
public sealed class DriveSpacesController : ControllerBase
{
    private readonly ISender _sender;

    public DriveSpacesController(ISender sender) => _sender = sender;

    /// <summary>Lists the caller's personal space and the shared spaces they can reach.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The personal space (if any) plus reachable shared spaces.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DRIVE_USE.</response>
    [HttpGet]
    [EndpointSummary("Lists the caller's personal space and the shared spaces they can reach.")]
    [EndpointDescription("Returns the caller's personal space «ملفاتي» (when it exists) and every shared space they own or hold a grant on. Requires the DRIVE_USE permission. Responses: 200 (The personal space plus reachable shared spaces.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DRIVE_USE.)")]
    [ProducesResponseType<DriveSpacesOverviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => (await _sender.Send(new ListDriveSpacesQuery(), cancellationToken)).ToActionResult();

    /// <summary>Creates a shared space owned by the caller.</summary>
    /// <param name="request">The space name and an optional quota override (ADMIN only).</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created space.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DRIVE_USE, or a non-ADMIN supplied a quota override.</response>
    [HttpPost]
    [EndpointSummary("Creates a shared space owned by the caller.")]
    [EndpointDescription("Creates a shared Drive space owned by the caller. A quota override requires DRIVE_MANAGE (ADMIN); otherwise the institution default applies. Requires the DRIVE_USE permission. Responses: 200 (The created space.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DRIVE_USE, or a non-ADMIN supplied a quota override.)")]
    [ProducesResponseType<DriveSpaceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDriveSpaceRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new CreateDriveSpaceCommand(request.Name, request.QuotaBytes), cancellationToken)).ToActionResult();

    /// <summary>Ensures the caller's personal space exists, creating it on first use.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The caller's personal space (created if absent).</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DRIVE_USE.</response>
    [HttpPost("personal")]
    [EndpointSummary("Ensures the caller's personal space exists, creating it on first use.")]
    [EndpointDescription("Idempotently returns the caller's personal space «ملفاتي», creating it the first time it is requested. Requires the DRIVE_USE permission. Responses: 200 (The caller's personal space.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DRIVE_USE.)")]
    [ProducesResponseType<DriveSpaceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> EnsurePersonal(CancellationToken cancellationToken)
        => (await _sender.Send(new EnsurePersonalDriveSpaceCommand(), cancellationToken)).ToActionResult();

    /// <summary>Reads one space the caller owns or can reach.</summary>
    /// <param name="spaceId">Space unique identifier (GUID, route). Example: <c>3f2a9c14-7d58-4e62-9b10-2c84f6a7d901</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The space with its effective quota and usage.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DRIVE_USE, or the caller cannot reach the space.</response>
    /// <response code="404">No space with that id exists.</response>
    [HttpGet("{spaceId:guid}")]
    [EndpointSummary("Reads one space the caller owns or can reach.")]
    [EndpointDescription("Returns a single Drive space with its effective quota and current usage. The caller must own it, hold a grant on it, or be ADMIN. Requires the DRIVE_USE permission. Responses: 200 (The space with its effective quota and usage.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DRIVE_USE, or the caller cannot reach the space.), 404 (No space with that id exists.)")]
    [ProducesResponseType<DriveSpaceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(
        [Description("Space unique identifier (GUID, route). Example: 3f2a9c14-7d58-4e62-9b10-2c84f6a7d901.")] Guid spaceId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new GetDriveSpaceQuery(spaceId), cancellationToken)).ToActionResult();

    /// <summary>Renames a space (owner or ADMIN).</summary>
    /// <param name="spaceId">Space unique identifier (GUID, route). Example: <c>3f2a9c14-7d58-4e62-9b10-2c84f6a7d901</c>.</param>
    /// <param name="request">The new space name.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The renamed space.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller is neither the owner nor ADMIN.</response>
    /// <response code="404">No space with that id exists.</response>
    [HttpPut("{spaceId:guid}")]
    [EndpointSummary("Renames a space (owner or ADMIN).")]
    [EndpointDescription("Renames a Drive space. Only the space owner or an ADMIN may rename it. Requires the DRIVE_USE permission. Responses: 200 (The renamed space.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The caller is neither the owner nor ADMIN.), 404 (No space with that id exists.)")]
    [ProducesResponseType<DriveSpaceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rename(
        [Description("Space unique identifier (GUID, route). Example: 3f2a9c14-7d58-4e62-9b10-2c84f6a7d901.")] Guid spaceId,
        [FromBody] RenameDriveSpaceRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new RenameDriveSpaceCommand(spaceId, request.Name), cancellationToken)).ToActionResult();

    /// <summary>Sets a per-space quota override (ADMIN); null restores the institution default.</summary>
    /// <param name="spaceId">Space unique identifier (GUID, route). Example: <c>3f2a9c14-7d58-4e62-9b10-2c84f6a7d901</c>.</param>
    /// <param name="request">The quota in bytes, or null to use the institution default.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The space with its updated effective quota.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks DRIVE_MANAGE.</response>
    /// <response code="404">No space with that id exists.</response>
    /// <response code="409">The quota is below the space's current usage.</response>
    [HttpPut("{spaceId:guid}/quota")]
    [Authorize(Policy = Permissions.DriveManage)]
    [EndpointSummary("Sets a per-space quota override (ADMIN); null restores the institution default.")]
    [EndpointDescription("Sets or clears a space's quota override. A quota below the space's current usage is rejected. Requires the DRIVE_MANAGE permission. Responses: 200 (The space with its updated effective quota.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks DRIVE_MANAGE.), 404 (No space with that id exists.), 409 (The quota is below the space's current usage.)")]
    [ProducesResponseType<DriveSpaceResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SetQuota(
        [Description("Space unique identifier (GUID, route). Example: 3f2a9c14-7d58-4e62-9b10-2c84f6a7d901.")] Guid spaceId,
        [FromBody] SetDriveSpaceQuotaRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new SetDriveSpaceQuotaCommand(spaceId, request.QuotaBytes), cancellationToken)).ToActionResult();
}