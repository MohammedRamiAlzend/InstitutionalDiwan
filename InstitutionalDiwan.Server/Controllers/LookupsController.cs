using InstitutionalDiwan.Application.Features.Lookups;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Server.Contracts.Lookups;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Serves the seeded reference ("lookup") catalogs through one generic route:
/// <c>/api/lookups/{lookup}</c> where <c>{lookup}</c> is the kebab-case catalog key
/// (for example <c>document-types</c>, <c>movement-statuses</c>, <c>audit-action-types</c>).
/// </summary>
[ApiController]
[Route("api/lookups")]
[Produces("application/json")]
[Authorize]
public sealed class LookupsController : ControllerBase
{
    private readonly ISender _sender;

    public LookupsController(ISender sender) => _sender = sender;

    /// <summary>Lists all supported lookup catalog keys.</summary>
    /// <response code="200">A list of lookup catalog slugs that can be used as the <c>{lookup}</c> segment.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    [HttpGet]
    [EndpointSummary("Lists all supported lookup catalog keys.")]
    [EndpointDescription("Lists all supported lookup catalog keys. Requires a valid access token. Responses: 200 (A list of lookup catalog slugs that can be used as the {lookup} segment.), 401 (The token is missing, expired, or invalid.)")]
    [ProducesResponseType<string[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult ListAllCatalogs()
    {
        var keys = LookupCatalog.Definitions.Keys.ToList();
        return Ok(keys);
    }

    /// <summary>Lists a reference catalog, ordered by id.</summary>
    /// <param name="lookup">Lookup catalog key (string, route). Example: <c>document-types</c>.</param>
    /// <param name="includeInactive">Include inactive records (boolean, optional, query parameter, default <c>false</c>). Example: <c>true</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The catalog entries (active only unless <c>includeInactive</c> is true).</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="404">No lookup catalog matches the supplied slug.</response>
    [HttpGet("{lookup}")]
    [EndpointSummary("Lists a reference catalog, ordered by id.")]
    [EndpointDescription("Lists a reference catalog, ordered by id. Requires a valid access token. Responses: 200 (The catalog entries (active only unless includeInactive is true).), 401 (The token is missing, expired, or invalid.), 404 (No lookup catalog matches the supplied slug.)")]
    [ProducesResponseType<IReadOnlyCollection<LookupEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [Description("Lookup catalog key (string, route). Example: document-types.")] string lookup,
        [Description("Include inactive records (boolean, optional, query parameter, default false). Example: true.")] [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListLookupEntriesQuery(lookup, includeInactive), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches one catalog entry (active or inactive) by id.</summary>
    /// <param name="lookup">Lookup catalog key (string, route). Example: <c>document-types</c>.</param>
    /// <param name="id">Catalog entry id (integer, route). Example: <c>101</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The catalog entry.</response>
    /// <response code="404">The catalog slug is unknown or the entry id does not exist.</response>
    [HttpGet("{lookup}/{id:int}")]
    [EndpointSummary("Fetches one catalog entry (active or inactive) by id.")]
    [EndpointDescription("Fetches one catalog entry (active or inactive) by id. Requires a valid access token. Responses: 200 (The catalog entry.), 404 (The catalog slug is unknown or the entry id does not exist.)")]
    [ProducesResponseType<LookupEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [Description("Lookup catalog key (string, route). Example: document-types.")] string lookup,
        [Description("Catalog entry id (integer, route). Example: 101.")] int id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLookupEntryByIdQuery(lookup, id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Adds a new entry to a catalog using an administrator-chosen stable id.</summary>
    /// <param name="lookup">Lookup catalog key (string, route). Example: <c>document-types</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>id</term><description>Catalog entry id (integer, required). Example: <c>101</c>.</description></item>
    /// <item><term>desc</term><description>English label (string, required). Example: <c>Finance Department</c>.</description></item>
    /// <item><term>descAr</term><description>Arabic label (string, required). Example: <c>الإدارة المالية</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created catalog entry.</response>
    /// <response code="400">The payload failed validation or the catalog cannot be extended through this endpoint.</response>
    /// <response code="403">The token lacks the LOOKUP_MANAGE permission.</response>
    /// <response code="404">No lookup catalog matches the supplied slug.</response>
    /// <response code="409">An entry with that id already exists in the catalog.</response>
    [HttpPost("{lookup}")]
    [EndpointSummary("Adds a new entry to a catalog using an administrator-chosen stable id.")]
    [EndpointDescription("Adds a new entry to a catalog using an administrator-chosen stable id. Requires the LOOKUP_MANAGE permission. Responses: 200 (The created catalog entry.), 400 (The payload failed validation or the catalog cannot be extended through this endpoint.), 403 (The token lacks the LOOKUP_MANAGE permission.), 404 (No lookup catalog matches the supplied slug.), 409 (An entry with that id already exists in the catalog.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<LookupEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [Description("Lookup catalog key (string, route). Example: document-types.")] string lookup,
        [FromBody] CreateLookupEntryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateLookupEntryCommand(lookup, request.Id, request.Desc, request.DescAr),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Updates the English and Arabic labels of an existing catalog entry.</summary>
    /// <param name="lookup">Lookup catalog key (string, route). Example: <c>document-types</c>.</param>
    /// <param name="id">Catalog entry id (integer, route). Example: <c>101</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>desc</term><description>English label (string, required). Example: <c>Finance Department</c>.</description></item>
    /// <item><term>descAr</term><description>Arabic label (string, required). Example: <c>الإدارة المالية</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The relabelled entry.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="403">The token lacks the LOOKUP_MANAGE permission.</response>
    /// <response code="404">The catalog slug is unknown or the entry id does not exist.</response>
    [HttpPut("{lookup}/{id:int}")]
    [EndpointSummary("Updates the English and Arabic labels of an existing catalog entry.")]
    [EndpointDescription("Updates the English and Arabic labels of an existing catalog entry. Requires the LOOKUP_MANAGE permission. Responses: 200 (The relabelled entry.), 400 (The payload failed validation.), 403 (The token lacks the LOOKUP_MANAGE permission.), 404 (The catalog slug is unknown or the entry id does not exist.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<LookupEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Relabel(
        [Description("Lookup catalog key (string, route). Example: document-types.")] string lookup,
        [Description("Catalog entry id (integer, route). Example: 101.")] int id,
        [FromBody] RelabelLookupEntryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RelabelLookupEntryCommand(lookup, id, request.Desc, request.DescAr),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Soft-deletes a catalog entry so it can no longer be selected.</summary>
    /// <param name="lookup">Lookup catalog key (string, route). Example: <c>document-types</c>.</param>
    /// <param name="id">Catalog entry id (integer, route). Example: <c>101</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The deactivated entry.</response>
    /// <response code="403">The token lacks the LOOKUP_MANAGE permission.</response>
    /// <response code="404">The catalog slug is unknown or the entry id does not exist.</response>
    /// <response code="409">Other records still reference the entry.</response>
    [HttpPatch("{lookup}/{id:int}/deactivate")]
    [EndpointSummary("Soft-deletes a catalog entry so it can no longer be selected.")]
    [EndpointDescription("Soft-deletes a catalog entry so it can no longer be selected. Requires the LOOKUP_MANAGE permission. Responses: 200 (The deactivated entry.), 403 (The token lacks the LOOKUP_MANAGE permission.), 404 (The catalog slug is unknown or the entry id does not exist.), 409 (Other records still reference the entry.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<LookupEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(
        [Description("Lookup catalog key (string, route). Example: document-types.")] string lookup,
        [Description("Catalog entry id (integer, route). Example: 101.")] int id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateLookupEntryCommand(lookup, id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Re-enables a previously deactivated catalog entry.</summary>
    /// <param name="lookup">Lookup catalog key (string, route). Example: <c>document-types</c>.</param>
    /// <param name="id">Catalog entry id (integer, route). Example: <c>101</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The reactivated entry.</response>
    /// <response code="403">The token lacks the LOOKUP_MANAGE permission.</response>
    /// <response code="404">The catalog slug is unknown or the entry id does not exist.</response>
    [HttpPatch("{lookup}/{id:int}/activate")]
    [EndpointSummary("Re-enables a previously deactivated catalog entry.")]
    [EndpointDescription("Re-enables a previously deactivated catalog entry. Requires the LOOKUP_MANAGE permission. Responses: 200 (The reactivated entry.), 403 (The token lacks the LOOKUP_MANAGE permission.), 404 (The catalog slug is unknown or the entry id does not exist.)")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<LookupEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        [Description("Lookup catalog key (string, route). Example: document-types.")] string lookup,
        [Description("Catalog entry id (integer, route). Example: 101.")] int id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateLookupEntryCommand(lookup, id), cancellationToken);
        return result.ToActionResult();
    }
}
