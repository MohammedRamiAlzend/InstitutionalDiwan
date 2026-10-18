using InstitutionalDiwan.Application.Features.Identity.Permissions;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Exposes the permission catalog so an administrator can pick the codes to grant to a role.
/// Requires the <c>ROLE_MANAGE</c> permission.
/// </summary>
[ApiController]
[Route("api/permissions")]
[Produces("application/json")]
[Authorize(Policy = Permissions.RoleManage)]
public sealed class PermissionsController : ControllerBase
{
    private readonly ISender _sender;

    public PermissionsController(ISender sender) => _sender = sender;

    /// <summary>Lists every permission in the catalog, ordered by module.</summary>
    /// <response code="200">The permission catalog.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the ROLE_MANAGE permission.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<PermissionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListPermissionsQuery(), cancellationToken);
        return result.ToActionResult();
    }
}
