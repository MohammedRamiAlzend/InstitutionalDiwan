using InstitutionalDiwan.Application.Features.Identity.Roles;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Identity;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Role management (CRUD + permission grants). Every action requires the <c>ROLE_MANAGE</c> permission.
/// </summary>
[ApiController]
[Route("api/roles")]
[Produces("application/json")]
[Authorize(Policy = Permissions.RoleManage)]
public sealed class RolesController : ControllerBase
{
    private readonly ISender _sender;

    public RolesController(ISender sender) => _sender = sender;

    /// <summary>Lists every role with its granted permission codes.</summary>
    /// <response code="200">The role list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the ROLE_MANAGE permission.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<RoleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListRolesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches a role, including its granted permission codes, by id.</summary>
    /// <response code="200">The role.</response>
    /// <response code="404">No role with that id exists.</response>
    [HttpGet("{roleId:guid}")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid roleId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRoleByIdQuery(roleId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a new (non-system) role.</summary>
    /// <response code="200">The created role.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="409">A role with the same code already exists.</response>
    [HttpPost]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateRoleCommand(
            request.Code,
            request.NameAr,
            request.NameEn,
            request.Description), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Updates a role's display names and description. The code is immutable.</summary>
    /// <response code="200">The updated role.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="404">No role with that id exists.</response>
    [HttpPut("{roleId:guid}")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid roleId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateRoleCommand(
            roleId,
            request.NameAr,
            request.NameEn,
            request.Description), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Soft-deletes a role by deactivating it. System roles are rejected with 403.</summary>
    /// <response code="200">The role was deactivated.</response>
    /// <response code="403">The role is system-defined and cannot be deactivated.</response>
    /// <response code="404">No role with that id exists.</response>
    [HttpDelete("{roleId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid roleId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteRoleCommand(roleId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated role.</summary>
    /// <response code="200">The reactivated role.</response>
    /// <response code="404">No role with that id exists.</response>
    [HttpPost("{roleId:guid}/activate")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid roleId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateRoleCommand(roleId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Grants a permission from the catalog to a role.</summary>
    /// <response code="200">The role with its updated permission codes.</response>
    /// <response code="400">The payload failed validation, or the permission does not exist.</response>
    /// <response code="404">No role with that id exists.</response>
    /// <response code="409">The role already holds the permission.</response>
    [HttpPost("{roleId:guid}/permissions")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GrantPermission(
        Guid roleId,
        [FromBody] GrantPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GrantRolePermissionCommand(roleId, request.PermissionCode), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Revokes a permission from a role. Revoking a permission the role does not hold is a no-op.</summary>
    /// <response code="200">The role with its updated permission codes.</response>
    /// <response code="404">No role with that id exists.</response>
    [HttpDelete("{roleId:guid}/permissions/{permissionCode}")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokePermission(
        Guid roleId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RevokeRolePermissionCommand(roleId, permissionCode), cancellationToken);

        return result.ToActionResult();
    }
}
