using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Drive;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Module 11 (Drive) — institution-wide Drive defaults: default space quota, maximum file size,
/// and the allowed-extension allow-list. Read and edit require DRIVE_MANAGE.
/// </summary>
[ApiController]
[Route("api/drive/settings")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DriveManage)]
public sealed class DriveSettingsController : ControllerBase
{
    private readonly ISender _sender;

    public DriveSettingsController(ISender sender) => _sender = sender;

    /// <summary>Reads the institution-wide Drive defaults.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The default quota, max file size, and allowed extensions.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks the DRIVE_MANAGE permission.</response>
    [HttpGet]
    [EndpointSummary("Reads the institution-wide Drive defaults.")]
    [EndpointDescription("Returns the default space quota, maximum file size, and allowed-extension CSV. Requires the DRIVE_MANAGE permission. Responses: 200 (The Drive defaults.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks the DRIVE_MANAGE permission.)")]
    [ProducesResponseType<DriveSettingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
        => (await _sender.Send(new GetDriveSettingsQuery(), cancellationToken)).ToActionResult();

    /// <summary>Updates the institution-wide Drive defaults.</summary>
    /// <param name="request">The new default quota, max file size, and allowed-extension CSV.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated Drive defaults.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks the DRIVE_MANAGE permission.</response>
    [HttpPut]
    [EndpointSummary("Updates the institution-wide Drive defaults.")]
    [EndpointDescription("Updates the default space quota, maximum file size, and allowed-extension CSV. Requires the DRIVE_MANAGE permission. Responses: 200 (The updated Drive defaults.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks the DRIVE_MANAGE permission.)")]
    [ProducesResponseType<DriveSettingsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateDriveSettingsRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new UpdateDriveSettingsCommand(
            request.DefaultQuotaBytes, request.MaxFileSizeBytes, request.AllowedExtensionsCsv), cancellationToken)).ToActionResult();
}