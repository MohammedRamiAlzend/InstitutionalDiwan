using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Identity.Users;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Identity;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// User administration (CRUD, activation, and role grants). Every action requires the
/// <c>USER_MANAGE</c> permission.
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
[Authorize(Policy = Permissions.UserManage)]
public sealed class UsersController : ControllerBase
{
    private readonly ISender _sender;

    public UsersController(ISender sender) => _sender = sender;

    /// <summary>Lists a page of user accounts with their roles.</summary>
    /// <remarks>
    /// Supports server-side paging and a username search. Requires the <c>USER_MANAGE</c> permission.
    /// </remarks>
    /// <param name="page">Page number (integer, optional, 1-based). Example: <c>2</c>.</param>
    /// <param name="pageSize">Rows per page (integer, optional, 1-100). Example: <c>20</c>.</param>
    /// <param name="search">Username search term (string, optional). Example: <c>admin</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">One page of users with the paging counters.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token does not carry the USER_MANAGE permission.</response>
    [HttpGet]
    [EndpointSummary("Lists a page of user accounts with their roles.")]
    [EndpointDescription("Lists a page of user accounts with their roles. Supports server-side paging and a username search. Requires the USER_MANAGE permission. Responses: 200 (One page of users with the paging counters.), 401 (The token is missing, expired, or invalid.), 403 (The token does not carry the USER_MANAGE permission.)")]
    [Produces("application/json", Type = typeof(PagedResult<UserResponse>))]
    [ProducesResponseType<PagedResult<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [Description("Page number (integer, optional, 1-based). Example: 2.")] [FromQuery] int page = 1,
        [Description("Rows per page (integer, optional, 1-100). Example: 20.")] [FromQuery] int pageSize = 20,
        [Description("Username search term (string, optional). Example: admin.")] [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new ListUsersQuery(page, pageSize, search), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Fetches a user account by id.</summary>
    /// <param name="userId">User unique identifier (GUID, route). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The user.</response>
    /// <response code="404">No user with that id exists.</response>
    [HttpGet("{userId:guid}")]
    [EndpointSummary("Fetches a user account by id.")]
    [EndpointDescription("Fetches a user account by id. Requires the USER_MANAGE permission. Responses: 200 (The user.), 404 (No user with that id exists.)")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([Description("User unique identifier (GUID, route). Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921.")] Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserByIdQuery(userId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a user account (the raw password is hashed server-side).</summary>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>username</term><description>Unique username for login. Example: ahmed.khalil</description></item>
    /// <item><term>password</term><description>Account password. Must meet password complexity requirements. Example: TempP@ss123!</description></item>
    /// <item><term>primaryDepartmentId</term><description>GUID of the user's primary department. Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42</description></item>
    /// <item><term>employeeId</term><description>Optional GUID linking the user to an employee record. Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921</description></item>
    /// <item><term>scopeTypeId</term><description>User scope type identifier. Default is 1 (Department scope). Example: 1</description></item>
    /// <item><term>mustChangePassword</term><description>Whether the user must change password on next login. Default is true. Example: true</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created user.</response>
    /// <response code="400">The payload was invalid.</response>
    /// <response code="409">The username is already taken.</response>
    [HttpPost]
    [EndpointSummary("Creates a user account (the raw password is hashed server-side).")]
    [EndpointDescription("Creates a user account (the raw password is hashed server-side). Requires the USER_MANAGE permission. Responses: 200 (The created user.), 400 (The payload was invalid.), 409 (The username is already taken.)")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateUserCommand(
            request.Username,
            request.Password,
            request.PrimaryDepartmentId,
            request.EmployeeId,
            request.ScopeTypeId,
            request.MustChangePassword), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Updates a user's department, employee link, scope, and password-change flag.</summary>
    /// <param name="userId">User unique identifier (GUID, route). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>primaryDepartmentId</term><description>Primary department unique identifier (GUID, required). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</description></item>
    /// <item><term>employeeId</term><description>Employee unique identifier (GUID, optional). Example: <c>3fa85f64-5717-4562-b3fc-2c963f66afa6</c>.</description></item>
    /// <item><term>scopeTypeId</term><description>Scope type id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>mustChangePassword</term><description>Must change password (boolean, required). Example: <c>true</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated user.</response>
    /// <response code="400">The payload was invalid.</response>
    /// <response code="404">No user with that id exists.</response>
    [HttpPut("{userId:guid}")]
    [EndpointSummary("Updates a user's department, employee link, scope, and password-change flag.")]
    [EndpointDescription("Updates a user's department, employee link, scope, and password-change flag. Requires the USER_MANAGE permission. Responses: 200 (The updated user.), 400 (The payload was invalid.), 404 (No user with that id exists.)")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [Description("User unique identifier (GUID, route). Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921.")] Guid userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateUserCommand(
            userId,
            request.PrimaryDepartmentId,
            request.EmployeeId,
            request.ScopeTypeId,
            request.MustChangePassword), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Soft-deletes a user account by deactivating it.</summary>
    /// <param name="userId">User unique identifier (GUID, route). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The deactivated user.</response>
    /// <response code="404">No user with that id exists.</response>
    [HttpDelete("{userId:guid}")]
    [EndpointSummary("Soft-deletes a user account by deactivating it.")]
    [EndpointDescription("Soft-deletes a user account by deactivating it. Requires the USER_MANAGE permission. Responses: 200 (The deactivated user.), 404 (No user with that id exists.)")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([Description("User unique identifier (GUID, route). Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921.")] Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateUserCommand(userId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated user.</summary>
    /// <param name="userId">User unique identifier (GUID, route). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The reactivated user.</response>
    /// <response code="404">No user with that id exists.</response>
    [HttpPost("{userId:guid}/activate")]
    [EndpointSummary("Reactivates a previously deactivated user.")]
    [EndpointDescription("Reactivates a previously deactivated user. Requires the USER_MANAGE permission. Responses: 200 (The reactivated user.), 404 (No user with that id exists.)")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate([Description("User unique identifier (GUID, route). Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921.")] Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateUserCommand(userId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Grants a role to a user, recording the caller as the grantor.</summary>
    /// <param name="userId">User unique identifier (GUID, route). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>roleId</term><description>Role unique identifier (GUID, required). Example: <c>9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated user.</response>
    /// <response code="404">No user or role with that id exists.</response>
    /// <response code="409">The role is already assigned to the user.</response>
    [HttpPost("{userId:guid}/roles")]
    [EndpointSummary("Grants a role to a user, recording the caller as the grantor.")]
    [EndpointDescription("Grants a role to a user, recording the caller as the grantor. Requires the USER_MANAGE permission. Responses: 200 (The updated user.), 404 (No user or role with that id exists.), 409 (The role is already assigned to the user.)")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignRole(
        [Description("User unique identifier (GUID, route). Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921.")] Guid userId,
        [FromBody] AssignUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        var assignedByUserId = CurrentUserId;
        if (assignedByUserId is null)
            return Unauthorized();

        var result = await _sender.Send(
            new AssignUserRoleCommand(userId, request.RoleId, assignedByUserId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Revokes a role from a user.</summary>
    /// <param name="userId">User unique identifier (GUID, route). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</param>
    /// <param name="roleId">Role unique identifier (GUID, route). Example: <c>9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated user.</response>
    /// <response code="404">No user with that id exists, or the role is not assigned.</response>
    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    [EndpointSummary("Revokes a role from a user.")]
    [EndpointDescription("Revokes a role from a user. Requires the USER_MANAGE permission. Responses: 200 (The updated user.), 404 (No user with that id exists, or the role is not assigned.)")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeRole(
        [Description("User unique identifier (GUID, route). Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921.")] Guid userId,
        [Description("Role unique identifier (GUID, route). Example: 9d1c7a4e-4b2f-4a86-93d5-1f0e6b3c8a72.")] Guid roleId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RevokeUserRoleCommand(userId, roleId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Starts a temporal department assignment, widening the user's organizational scope. Set
    /// <c>includeSubtree</c> to cover the department's descendants as well.
    /// </summary>
    /// <param name="userId">User unique identifier (GUID, route). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>departmentId</term><description>Department unique identifier (GUID, required). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</description></item>
    /// <item><term>assignmentTypeId</term><description>Assignment type id (integer, optional, default <c>0</c>). Example: <c>1</c>.</description></item>
    /// <item><term>includeSubtree</term><description>Include subtree (boolean, optional, default <c>false</c>). Example: <c>true</c>.</description></item>
    /// <item><term>startDate</term><description>Start date (date, optional). Example: <c>2026-01-01</c>.</description></item>
    /// <item><term>endDate</term><description>End date (date, optional). Example: <c>2026-12-31</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated user.</response>
    /// <response code="400">The assignment type is unknown/inactive, or the date range is invalid.</response>
    /// <response code="404">No user or department with that id exists.</response>
    /// <response code="409">The user already has an active assignment for that department.</response>
    [HttpPost("{userId:guid}/department-assignments")]
    [EndpointSummary("Starts a temporal department assignment, widening the user's organizational scope. Set includeSubtree to cover the department's descendants as well.")]
    [EndpointDescription("Starts a temporal department assignment, widening the user's organizational scope. Set includeSubtree to cover the department's descendants as well. Requires the USER_MANAGE permission. Responses: 200 (The updated user.), 400 (The assignment type is unknown/inactive, or the date range is invalid.), 404 (No user or department with that id exists.), 409 (The user already has an active assignment for that department.)")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignDepartmentScope(
        [Description("User unique identifier (GUID, route). Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921.")] Guid userId,
        [FromBody] AssignUserDepartmentScopeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new AssignUserDepartmentScopeCommand(
            userId,
            request.DepartmentId,
            request.AssignmentTypeId,
            request.IncludeSubtree,
            request.StartDate,
            request.EndDate), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Ends a department assignment, keeping the row for auditing.</summary>
    /// <param name="userId">User unique identifier (GUID, route). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</param>
    /// <param name="assignmentId">Assignment unique identifier (GUID, route). Example: <c>d4b7f9c2-8e15-4a63-b2d7-5c0e9f8a1b46</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated user.</response>
    /// <response code="404">No user with that id exists, or the assignment does not belong to them.</response>
    [HttpDelete("{userId:guid}/department-assignments/{assignmentId:guid}")]
    [EndpointSummary("Ends a department assignment, keeping the row for auditing.")]
    [EndpointDescription("Ends a department assignment, keeping the row for auditing. Requires the USER_MANAGE permission. Responses: 200 (The updated user.), 404 (No user with that id exists, or the assignment does not belong to them.)")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EndDepartmentScope(
        [Description("User unique identifier (GUID, route). Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921.")] Guid userId,
        [Description("Assignment unique identifier (GUID, route). Example: d4b7f9c2-8e15-4a63-b2d7-5c0e9f8a1b46.")] Guid assignmentId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new EndUserDepartmentScopeCommand(userId, assignmentId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Resolves the user's effective permissions, role codes, scope type, and active department
    /// assignments — the same permission set their access token carries.
    /// </summary>
    /// <param name="userId">User unique identifier (GUID, route). Example: <c>5f8b32a7-c941-4e62-bd07-2c86ea754921</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The effective authorization state.</response>
    /// <response code="404">No user with that id exists.</response>
    [HttpGet("{userId:guid}/effective-permissions")]
    [EndpointSummary("Resolves the user's effective permissions, role codes, scope type, and active department assignments — the same permission set their access token carries.")]
    [EndpointDescription("Resolves the user's effective permissions, role codes, scope type, and active department assignments — the same permission set their access token carries. Requires the USER_MANAGE permission. Responses: 200 (The effective authorization state.), 404 (No user with that id exists.)")]
    [ProducesResponseType<UserEffectivePermissionsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEffectivePermissions([Description("User unique identifier (GUID, route). Example: 5f8b32a7-c941-4e62-bd07-2c86ea754921.")] Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetUserEffectivePermissionsQuery(userId), cancellationToken);

        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
