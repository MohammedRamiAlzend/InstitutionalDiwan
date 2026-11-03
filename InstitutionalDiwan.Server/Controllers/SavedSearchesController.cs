using InstitutionalDiwan.Application.Features.Archiving.SavedSearches;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Archiving;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>Module 07: the caller's personal saved archive searches. Another user's search answers 404.</summary>
[ApiController]
[Route("api/saved-searches")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DocumentView)]
public sealed class SavedSearchesController : ControllerBase
{
    private readonly ISender _sender;

    public SavedSearchesController(ISender sender) => _sender = sender;

    /// <summary>Lists the caller's saved searches, ordered by name.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The caller's saved searches.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    [HttpGet]
    [EndpointSummary("Lists the caller's saved searches, ordered by name.")]
    [EndpointDescription("Lists the caller's saved searches, ordered by name. Requires the DOCUMENT_VIEW permission. Responses: 200 (The caller's saved searches.), 403 (The token lacks the DOCUMENT_VIEW permission.)")]
    [ProducesResponseType<IReadOnlyCollection<SavedSearchResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new ListSavedSearchesQuery(userId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Saves an archive search for the caller.</summary>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>name</term><description>Name, unique for the caller, at most 150 characters (string, required). Example: <c>أرشيف 2026</c>.</description></item>
    /// <item><term>criteriaJson</term><description>The search criteria as a JSON object string, at most 4000 characters (string, required). Example: <c>{"search":"2026","archiveLocationId":2}</c>.</description></item>
    /// <item><term>isDefault</term><description>Make this the caller's default, replacing the current one (boolean, optional, default <c>false</c>). Example: <c>true</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The saved search.</response>
    /// <response code="400">The name or criteria are missing or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    /// <response code="409">The name is taken or the caller already has 50 saved searches.</response>
    [HttpPost]
    [EndpointSummary("Saves an archive search for the caller.")]
    [EndpointDescription("Saves an archive search for the caller. Requires the DOCUMENT_VIEW permission. Responses: 200 (The saved search.), 400 (The name or criteria are missing or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission.), 409 (The name is taken or the caller already has 50 saved searches.)")]
    [ProducesResponseType<SavedSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] SaveSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new CreateSavedSearchCommand(userId, request.Name, request.CriteriaJson, request.IsDefault),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Replaces the name, criteria, and default flag of one of the caller's saved searches.</summary>
    /// <param name="savedSearchId">Saved search unique identifier (GUID, route). Example: <c>3f8a1c52-7d94-4e06-b2c1-9a6e5d7f4b18</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>name</term><description>Name, unique for the caller, at most 150 characters (string, required). Example: <c>أرشيف 2026</c>.</description></item>
    /// <item><term>criteriaJson</term><description>The search criteria as a JSON object string, at most 4000 characters (string, required). Example: <c>{"search":"2026","archiveLocationId":2}</c>.</description></item>
    /// <item><term>isDefault</term><description>Make this the caller's default, replacing the current one (boolean, optional, default <c>false</c>). Example: <c>true</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated search.</response>
    /// <response code="400">The name or criteria are missing or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    /// <response code="404">The caller has no saved search with that id.</response>
    /// <response code="409">The name is taken.</response>
    [HttpPut("{savedSearchId:guid}")]
    [EndpointSummary("Replaces the name, criteria, and default flag of one of the caller's saved searches.")]
    [EndpointDescription("Replaces the name, criteria, and default flag of one of the caller's saved searches. Requires the DOCUMENT_VIEW permission. Responses: 200 (The updated search.), 400 (The name or criteria are missing or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission.), 404 (The caller has no saved search with that id.), 409 (The name is taken.)")]
    [ProducesResponseType<SavedSearchResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [Description("Saved search unique identifier (GUID, route). Example: 3f8a1c52-7d94-4e06-b2c1-9a6e5d7f4b18.")] Guid savedSearchId,
        [FromBody] SaveSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(
            new UpdateSavedSearchCommand(savedSearchId, userId, request.Name, request.CriteriaJson, request.IsDefault),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Permanently deletes one of the caller's saved searches.</summary>
    /// <param name="savedSearchId">Saved search unique identifier (GUID, route). Example: <c>3f8a1c52-7d94-4e06-b2c1-9a6e5d7f4b18</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The search was deleted.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    /// <response code="404">The caller has no saved search with that id.</response>
    [HttpDelete("{savedSearchId:guid}")]
    [EndpointSummary("Permanently deletes one of the caller's saved searches.")]
    [EndpointDescription("Permanently deletes one of the caller's saved searches. Requires the DOCUMENT_VIEW permission. Responses: 200 (The search was deleted.), 403 (The token lacks the DOCUMENT_VIEW permission.), 404 (The caller has no saved search with that id.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [Description("Saved search unique identifier (GUID, route). Example: 3f8a1c52-7d94-4e06-b2c1-9a6e5d7f4b18.")] Guid savedSearchId,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } userId)
            return Unauthorized();

        var result = await _sender.Send(new DeleteSavedSearchCommand(savedSearchId, userId), cancellationToken);
        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
