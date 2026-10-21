using InstitutionalDiwan.Application.Features.Identity.Roles;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Identity;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

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
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The role list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the ROLE_MANAGE permission.</response>
    [HttpGet]
    [EndpointSummary("Lists every role with its granted permission codes.")]
    [EndpointDescription("Lists every role with its granted permission codes. Requires the ROLE_MANAGE permission. Responses: 200 (The role list.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the ROLE_MANAGE permission.)")]
    [ProducesResponseType<IReadOnlyCollection<RoleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListRolesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches a role, including its granted permission codes, by id.</summary>
    /// <param name="roleId">Role unique identifier (GUID, route). Example: <c>9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The role.</response>
    /// <response code="404">No role with that id exists.</response>
    [HttpGet("{roleId:guid}")]
    [EndpointSummary("Fetches a role, including its granted permission codes, by id.")]
    [EndpointDescription("Fetches a role, including its granted permission codes, by id. Requires the ROLE_MANAGE permission. Responses: 200 (The role.), 404 (No role with that id exists.)")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([Description("Role unique identifier (GUID, route). Example: 9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72.")] Guid roleId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRoleByIdQuery(roleId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a new (non-system) role.</summary>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>code</term><description>Code (string, required). Example: <c>CODE-01</c>.</description></item>
    /// <item><term>nameAr</term><description>Name in Arabic (string, required). Example: <c>الإدارة المالية</c>.</description></item>
    /// <item><term>nameEn</term><description>Name in English (string, optional). Example: <c>Finance Department</c>.</description></item>
    /// <item><term>description</term><description>Description (string, optional). Example: <c>Sample value</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created role.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="409">A role with the same code already exists.</response>
    [HttpPost]
    [EndpointSummary("Creates a new (non-system) role.")]
    [EndpointDescription("Creates a new (non-system) role. Requires the ROLE_MANAGE permission. Responses: 200 (The created role.), 400 (The payload failed validation.), 409 (A role with the same code already exists.)")]
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
    /// <param name="roleId">Role unique identifier (GUID, route). Example: <c>9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>nameAr</term><description>Name in Arabic (string, required). Example: <c>الإدارة المالية</c>.</description></item>
    /// <item><term>nameEn</term><description>Name in English (string, optional). Example: <c>Finance Department</c>.</description></item>
    /// <item><term>description</term><description>Description (string, optional). Example: <c>Sample value</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated role.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="404">No role with that id exists.</response>
    [HttpPut("{roleId:guid}")]
    [EndpointSummary("Updates a role's display names and description. The code is immutable.")]
    [EndpointDescription("Updates a role's display names and description. The code is immutable. Requires the ROLE_MANAGE permission. Responses: 200 (The updated role.), 400 (The payload failed validation.), 404 (No role with that id exists.)")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [Description("Role unique identifier (GUID, route). Example: 9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72.")] Guid roleId,
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
    /// <param name="roleId">Role unique identifier (GUID, route). Example: <c>9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The role was deactivated.</response>
    /// <response code="403">The role is system-defined and cannot be deactivated.</response>
    /// <response code="404">No role with that id exists.</response>
    [HttpDelete("{roleId:guid}")]
    [EndpointSummary("Soft-deletes a role by deactivating it. System roles are rejected with 403.")]
    [EndpointDescription("Soft-deletes a role by deactivating it. System roles are rejected with 403. Requires the ROLE_MANAGE permission. Responses: 200 (The role was deactivated.), 403 (The role is system-defined and cannot be deactivated.), 404 (No role with that id exists.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([Description("Role unique identifier (GUID, route). Example: 9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72.")] Guid roleId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteRoleCommand(roleId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated role.</summary>
    /// <param name="roleId">Role unique identifier (GUID, route). Example: <c>9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The reactivated role.</response>
    /// <response code="404">No role with that id exists.</response>
    [HttpPost("{roleId:guid}/activate")]
    [EndpointSummary("Reactivates a previously deactivated role.")]
    [EndpointDescription("Reactivates a previously deactivated role. Requires the ROLE_MANAGE permission. Responses: 200 (The reactivated role.), 404 (No role with that id exists.)")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate([Description("Role unique identifier (GUID, route). Example: 9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72.")] Guid roleId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateRoleCommand(roleId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Grants a permission from the catalog to a role.</summary>
    /// <param name="roleId">Role unique identifier (GUID, route). Example: <c>9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>permissionCode</term><description>Permission code (string, required). Example: <c>DOCUMENT_VIEW</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The role with its updated permission codes.</response>
    /// <response code="400">The payload failed validation, or the permission does not exist.</response>
    /// <response code="404">No role with that id exists.</response>
    /// <response code="409">The role already holds the permission.</response>
    [HttpPost("{roleId:guid}/permissions")]
    [EndpointSummary("Grants a permission from the catalog to a role.")]
    [EndpointDescription("Grants a permission from the catalog to a role. Requires the ROLE_MANAGE permission. Responses: 200 (The role with its updated permission codes.), 400 (The payload failed validation, or the permission does not exist.), 404 (No role with that id exists.), 409 (The role already holds the permission.)")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> GrantPermission(
        [Description("Role unique identifier (GUID, route). Example: 9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72.")] Guid roleId,
        [FromBody] GrantPermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GrantRolePermissionCommand(roleId, request.PermissionCode), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Revokes a permission from a role. Revoking a permission the role does not hold is a no-op.</summary>
    /// <param name="roleId">Role unique identifier (GUID, route). Example: <c>9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72</c>.</param>
    /// <param name="permissionCode">Permission code (string, route). Example: <c>DOCUMENT_VIEW</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The role with its updated permission codes.</response>
    /// <response code="404">No role with that id exists.</response>
    [HttpDelete("{roleId:guid}/permissions/{permissionCode}")]
    [EndpointSummary("Revokes a permission from a role. Revoking a permission the role does not hold is a no-op.")]
    [EndpointDescription("Revokes a permission from a role. Revoking a permission the role does not hold is a no-op. Requires the ROLE_MANAGE permission. Responses: 200 (The role with its updated permission codes.), 404 (No role with that id exists.)")]
    [ProducesResponseType<RoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokePermission(
        [Description("Role unique identifier (GUID, route). Example: 9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72.")] Guid roleId,
        [Description("Permission code (string, route). Example: DOCUMENT_VIEW.")] string permissionCode,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RevokeRolePermissionCommand(roleId, permissionCode), cancellationToken);

        return result.ToActionResult();
    }
}
