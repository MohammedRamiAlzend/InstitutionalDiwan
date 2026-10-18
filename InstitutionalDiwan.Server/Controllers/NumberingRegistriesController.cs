using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

[ApiController]
[Route("api/numbering/registries")]
[Produces("application/json")]
[Authorize(Policy = Permissions.NumberingView)]
public sealed class NumberingRegistriesController : ControllerBase
{
    private readonly ISender _sender;

    public NumberingRegistriesController(ISender sender) => _sender = sender;

    /// <summary>Lists every numbering registry with its id, scope, and counters.</summary>
    /// <response code="200">The numbering registry list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the NUMBERING_VIEW permission.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<NumberingRegistryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListRegistriesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches a single numbering registry by its id.</summary>
    /// <response code="200">The numbering registry.</response>
    /// <response code="404">No numbering registry with that id exists.</response>
    [HttpGet("{registryId:guid}")]
    [ProducesResponseType<NumberingRegistryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid registryId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRegistryByIdQuery(registryId), cancellationToken);
        return result.ToActionResult();
    }
}
