using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// <response code="200">The register lines.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the NUMBERING_VIEW permission.</response>
    /// <response code="404">No department with that id exists.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<DepartmentRegisterLineResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        Guid departmentId,
        [FromQuery] int? numberingYear,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ListDepartmentRegisterQuery(departmentId, numberingYear), cancellationToken);

        return result.ToActionResult();
    }
}
