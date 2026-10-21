using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// The register book of a department: documents written into that department's register, with the
/// department's own number and date and the central Diwan number kept alongside.
/// </summary>
[ApiController]
[Route("api/departments/{departmentId:guid}/register")]
[Produces("application/json")]
[Authorize(Policy = Permissions.NumberingView)]
public sealed class DepartmentRegistersController : ControllerBase
{
    private readonly ISender _sender;

    public DepartmentRegistersController(ISender sender) => _sender = sender;

    /// <summary>Lists a department's register entries ordered by register date.</summary>
    /// <param name="departmentId">Department unique identifier (GUID, route). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="numberingYear">Numbering year (integer, optional, query parameter). Example: <c>2026</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The register lines.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the NUMBERING_VIEW permission.</response>
    /// <response code="404">No department with that id exists.</response>
    [HttpGet]
    [EndpointSummary("Lists a department's register entries ordered by register date.")]
    [EndpointDescription("Lists a department's register entries ordered by register date. Requires the NUMBERING_VIEW permission. Responses: 200 (The register lines.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the NUMBERING_VIEW permission.), 404 (No department with that id exists.)")]
    [ProducesResponseType<IReadOnlyCollection<DepartmentRegisterLineResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [Description("Department unique identifier (GUID, route). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid departmentId,
        [Description("Numbering year (integer, optional, query parameter). Example: 2026.")] [FromQuery] int? numberingYear,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ListDepartmentRegisterQuery(departmentId, numberingYear), cancellationToken);

        return result.ToActionResult();
    }
}
