using InstitutionalDiwan.Application.Features.Identity.ListUsers;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize(Policy = Permissions.UserManage)]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    /// <summary>Lists all user accounts and their roles. Requires the <c>USER_MANAGE</c> permission.</summary>
    /// <response code="200">The user list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token does not carry the USER_MANAGE permission.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListUsersQuery(), cancellationToken);
        return result.ToActionResult();
    }
}
