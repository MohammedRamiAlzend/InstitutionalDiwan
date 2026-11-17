using InstitutionalDiwan.Application.Features.Ui.UiLabels;
using InstitutionalDiwan.Server.Authorization;
using InstitutionalDiwan.Server.Contracts.Ui;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Superadmin replacements for built-in UI labels (buttons, page titles, table columns, sidebar
/// entries). Any signed-in user reads the override set so the SPA can boot with it; only the
/// seeded superadmin account writes or resets a label.
/// </summary>
[ApiController]
[Route("api/ui-labels")]
[Produces("application/json")]
[Authorize]
public sealed class UiLabelsController : ControllerBase
{
    private readonly ISender _sender;

    public UiLabelsController(ISender sender) => _sender = sender;

    /// <summary>Returns every stored UI label replacement and the cache version.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The override set; <c>version</c> is 0 when nothing is overridden.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    [HttpGet]
    [EndpointSummary("Returns every stored UI label replacement and the cache version.")]
    [EndpointDescription("Returns every stored UI label replacement and the cache version. The version is the tick count of the newest write (0 when nothing is overridden), so clients can cache the set and revalidate cheaply. Requires authentication. Responses: 200 (The override set.), 401 (The token is missing, expired, or invalid.)")]
    [ProducesResponseType<UiLabelOverridesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUiLabelOverridesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates or updates the replacement for one UI label key.</summary>
    /// <param name="key">Label key (lowercase letters, digits, dots, dashes; 1-150 chars). Example: <c>nav.documents</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>value</term><description>Replacement text (string, required, at most 200 characters). Example: <c>الأضابير</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The full override set after the write.</response>
    /// <response code="400">The key or value failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller is not the superadmin account.</response>
    [HttpPut("{key}")]
    [EndpointSummary("Creates or updates the replacement for one UI label key.")]
    [EndpointDescription("Creates or updates the replacement for one UI label key. Only the seeded superadmin account may write. The value is at most 200 characters; placeholders such as {name} must match the built-in default (checked by the front end). Responses: 200 (The full override set after the write.), 400 (The key or value failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The caller is not the superadmin account.)")]
    [ProducesResponseType<UiLabelOverridesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Set(
        [Description("Label key (lowercase letters, digits, dots, dashes; 1-150 chars). Example: nav.documents.")] string key,
        [FromBody] SetUiLabelOverrideRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new SetUiLabelOverrideCommand(key, request.Value, CurrentUserId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Removes the replacement for one UI label key so the built-in default applies again.</summary>
    /// <param name="key">Label key. Example: <c>nav.documents</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The full override set after the reset; resetting an unknown key is a no-op.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller is not the superadmin account.</response>
    [HttpDelete("{key}")]
    [EndpointSummary("Removes the replacement for one UI label key.")]
    [EndpointDescription("Removes the replacement for one UI label key so the built-in default applies again. Only the seeded superadmin account may reset. Resetting a key that was never overridden is a successful no-op. Responses: 200 (The full override set after the reset.), 401 (The token is missing, expired, or invalid.), 403 (The caller is not the superadmin account.)")]
    [ProducesResponseType<UiLabelOverridesResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Reset(
        [Description("Label key. Example: nav.documents.")] string key,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ResetUiLabelOverrideCommand(key), cancellationToken);
        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}