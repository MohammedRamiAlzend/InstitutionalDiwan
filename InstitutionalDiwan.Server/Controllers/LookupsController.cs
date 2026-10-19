using InstitutionalDiwan.Application.Features.Lookups;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Infrastructure.Persistence.Repositories;
using InstitutionalDiwan.Server.Contracts.Lookups;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType<string[]>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult ListAllCatalogs()
    {
        var keys = LookupCatalog.Definitions.Keys.ToList();
        return Ok(keys);
    }

    /// <summary>Lists a reference catalog, ordered by id.</summary>
    /// <response code="200">The catalog entries (active only unless <c>includeInactive</c> is true).</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="404">No lookup catalog matches the supplied slug.</response>
    [HttpGet("{lookup}")]
    [ProducesResponseType<IReadOnlyCollection<LookupEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        string lookup,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListLookupEntriesQuery(lookup, includeInactive), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches one catalog entry (active or inactive) by id.</summary>
    /// <response code="200">The catalog entry.</response>
    /// <response code="404">The catalog slug is unknown or the entry id does not exist.</response>
    [HttpGet("{lookup}/{id:int}")]
    [ProducesResponseType<LookupEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        string lookup,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetLookupEntryByIdQuery(lookup, id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Adds a new entry to a catalog using an administrator-chosen stable id.</summary>
    /// <response code="200">The created catalog entry.</response>
    /// <response code="400">The payload failed validation or the catalog cannot be extended through this endpoint.</response>
    /// <response code="403">The token lacks the LOOKUP_MANAGE permission.</response>
    /// <response code="404">No lookup catalog matches the supplied slug.</response>
    /// <response code="409">An entry with that id already exists in the catalog.</response>
    [HttpPost("{lookup}")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<LookupEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        string lookup,
        [FromBody] CreateLookupEntryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new CreateLookupEntryCommand(lookup, request.Id, request.Desc, request.DescAr),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Updates the English and Arabic labels of an existing catalog entry.</summary>
    /// <response code="200">The relabelled entry.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="403">The token lacks the LOOKUP_MANAGE permission.</response>
    /// <response code="404">The catalog slug is unknown or the entry id does not exist.</response>
    [HttpPut("{lookup}/{id:int}")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<LookupEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Relabel(
        string lookup,
        int id,
        [FromBody] RelabelLookupEntryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RelabelLookupEntryCommand(lookup, id, request.Desc, request.DescAr),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Soft-deletes a catalog entry so it can no longer be selected.</summary>
    /// <response code="200">The deactivated entry.</response>
    /// <response code="403">The token lacks the LOOKUP_MANAGE permission.</response>
    /// <response code="404">The catalog slug is unknown or the entry id does not exist.</response>
    /// <response code="409">Other records still reference the entry.</response>
    [HttpPatch("{lookup}/{id:int}/deactivate")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<LookupEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Deactivate(
        string lookup,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateLookupEntryCommand(lookup, id), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Re-enables a previously deactivated catalog entry.</summary>
    /// <response code="200">The reactivated entry.</response>
    /// <response code="403">The token lacks the LOOKUP_MANAGE permission.</response>
    /// <response code="404">The catalog slug is unknown or the entry id does not exist.</response>
    [HttpPatch("{lookup}/{id:int}/activate")]
    [Authorize(Policy = Permissions.LookupManage)]
    [ProducesResponseType<LookupEntryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(
        string lookup,
        int id,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateLookupEntryCommand(lookup, id), cancellationToken);
        return result.ToActionResult();
    }
}
