using InstitutionalDiwan.Application.Features.Drive;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Drive;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// Module 11 (Drive) — explicit permissions on an item: list grants, grant/update a user or department
/// role, revoke a grant, read the caller's effective role, and break/restore inheritance.
/// Managing grants requires the Manager Drive role (enforced in the handlers).
/// </summary>
[ApiController]
[Route("api/drive/items")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DriveUse)]
public sealed class DrivePermissionsController : ControllerBase
{
    private readonly ISender _sender;

    public DrivePermissionsController(ISender sender) => _sender = sender;

    /// <summary>Lists the explicit grants on an item.</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The explicit grants (user and department) on the item.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Manager access.</response>
    /// <response code="404">No item with that id exists.</response>
    [HttpGet("{itemId:guid}/permissions")]
    [EndpointSummary("Lists the explicit grants on an item.")]
    [EndpointDescription("Returns the explicit user/department grants stored on the item (not the inherited effective role). Requires the DRIVE_USE permission and Manager access. Responses: 200 (The explicit grants on the item.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Manager access.), 404 (No item with that id exists.)")]
    [ProducesResponseType<IReadOnlyList<DrivePermissionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> List(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new ListDriveItemPermissionsQuery(itemId), cancellationToken)).ToActionResult();

    /// <summary>Grants (or updates) a user or department role on an item.</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="request">The principal type, principal id, and role to grant.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created or updated grant.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Manager access.</response>
    /// <response code="404">The item or principal does not exist.</response>
    [HttpPost("{itemId:guid}/permissions")]
    [EndpointSummary("Grants (or updates) a user or department role on an item.")]
    [EndpointDescription("Creates or updates an explicit grant for a user or department and notifies the grantee. Requires the DRIVE_USE permission and Manager access. Responses: 200 (The created or updated grant.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Manager access.), 404 (The item or principal does not exist.)")]
    [ProducesResponseType<DrivePermissionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Grant(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        [FromBody] GrantDrivePermissionRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new GrantDrivePermissionCommand(
            itemId, request.PrincipalTypeId, request.PrincipalId, request.RoleId), cancellationToken)).ToActionResult();

    /// <summary>Revokes an explicit grant.</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="permissionId">Permission unique identifier (GUID, route). Example: <c>1f3a6c84-2e57-4b09-9d12-7a48e5f60b31</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The grant is revoked.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Manager access.</response>
    /// <response code="404">No permission with that id exists on the item.</response>
    [HttpDelete("{itemId:guid}/permissions/{permissionId:guid}")]
    [EndpointSummary("Revokes an explicit grant.")]
    [EndpointDescription("Deletes an explicit grant on the item. Requires the DRIVE_USE permission and Manager access. Responses: 200 (The grant is revoked.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Manager access.), 404 (No permission with that id exists on the item.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Revoke(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        [Description("Permission unique identifier (GUID, route). Example: 1f3a6c84-2e57-4b09-9d12-7a48e5f60b31.")] Guid permissionId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new RevokeDrivePermissionCommand(permissionId), cancellationToken)).ToActionResult();

    /// <summary>Returns the caller's effective role on an item (inherited or explicit).</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The caller's effective role id.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="404">No item with that id exists.</response>
    [HttpGet("{itemId:guid}/effective")]
    [EndpointSummary("Returns the caller's effective role on an item (inherited or explicit).")]
    [EndpointDescription("Resolves the caller's effective Drive role on the item via the recursive permission walk (0 = no access). Requires the DRIVE_USE permission. Responses: 200 (The caller's effective role id.), 401 (The token is missing, expired, or invalid.), 404 (No item with that id exists.)")]
    [ProducesResponseType<DriveEffectiveRoleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Effective(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        CancellationToken cancellationToken)
        => (await _sender.Send(new GetDriveEffectiveRoleQuery(itemId), cancellationToken)).ToActionResult();

    /// <summary>Breaks or restores permission inheritance on an item.</summary>
    /// <param name="itemId">Item unique identifier (GUID, route). Example: <c>8c1d5e72-9a40-4f63-b217-5d84e6a7c902</c>.</param>
    /// <param name="request">Whether to break inheritance (true) or restore it (false).</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated item.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The caller lacks Manager access.</response>
    /// <response code="404">No item with that id exists.</response>
    [HttpPut("{itemId:guid}/inheritance")]
    [EndpointSummary("Breaks or restores permission inheritance on an item.")]
    [EndpointDescription("Toggles BreakInheritance: when broken the item stops inheriting ancestor grants and relies on its own explicit grants. Requires the DRIVE_USE permission and Manager access. Responses: 200 (The updated item.), 400 (The payload failed validation.), 401 (The token is missing, expired, or invalid.), 403 (The caller lacks Manager access.), 404 (No item with that id exists.)")]
    [ProducesResponseType<DriveItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetInheritance(
        [Description("Item unique identifier (GUID, route). Example: 8c1d5e72-9a40-4f63-b217-5d84e6a7c902.")] Guid itemId,
        [FromBody] SetDriveItemInheritanceRequest request,
        CancellationToken cancellationToken)
        => (await _sender.Send(new SetDriveItemInheritanceCommand(itemId, request.BreakInheritance), cancellationToken)).ToActionResult();
}