using System.Security.Claims;
using InstitutionalDiwan.Application.Features.Identity.Users;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Identity;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    /// <summary>Lists all user accounts and their roles.</summary>
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

    /// <summary>Fetches a user account by id.</summary>
    /// <response code="200">The user.</response>
    /// <response code="404">No user with that id exists.</response>
    [HttpGet("{userId:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserByIdQuery(userId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a user account (the raw password is hashed server-side).</summary>
    /// <response code="200">The created user.</response>
    /// <response code="400">The payload was invalid.</response>
    /// <response code="409">The username is already taken.</response>
    [HttpPost]
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
    /// <response code="200">The updated user.</response>
    /// <response code="400">The payload was invalid.</response>
    /// <response code="404">No user with that id exists.</response>
    [HttpPut("{userId:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid userId,
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
    /// <response code="200">The deactivated user.</response>
    /// <response code="404">No user with that id exists.</response>
    [HttpDelete("{userId:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateUserCommand(userId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated user.</summary>
    /// <response code="200">The reactivated user.</response>
    /// <response code="404">No user with that id exists.</response>
    [HttpPost("{userId:guid}/activate")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateUserCommand(userId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Grants a role to a user, recording the caller as the grantor.</summary>
    /// <response code="200">The updated user.</response>
    /// <response code="404">No user or role with that id exists.</response>
    /// <response code="409">The role is already assigned to the user.</response>
    [HttpPost("{userId:guid}/roles")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignRole(
        Guid userId,
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
    /// <response code="200">The updated user.</response>
    /// <response code="404">No user with that id exists, or the role is not assigned.</response>
    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeRole(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new RevokeUserRoleCommand(userId, roleId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Starts a temporal department assignment, widening the user's organizational scope. Set
    /// <c>includeSubtree</c> to cover the department's descendants as well.
    /// </summary>
    /// <response code="200">The updated user.</response>
    /// <response code="400">The assignment type is unknown/inactive, or the date range is invalid.</response>
    /// <response code="404">No user or department with that id exists.</response>
    /// <response code="409">The user already has an active assignment for that department.</response>
    [HttpPost("{userId:guid}/department-assignments")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignDepartmentScope(
        Guid userId,
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
    /// <response code="200">The updated user.</response>
    /// <response code="404">No user with that id exists, or the assignment does not belong to them.</response>
    [HttpDelete("{userId:guid}/department-assignments/{assignmentId:guid}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> EndDepartmentScope(
        Guid userId,
        Guid assignmentId,
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
    /// <response code="200">The effective authorization state.</response>
    /// <response code="404">No user with that id exists.</response>
    [HttpGet("{userId:guid}/effective-permissions")]
    [ProducesResponseType<UserEffectivePermissionsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEffectivePermissions(Guid userId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetUserEffectivePermissionsQuery(userId), cancellationToken);

        return result.ToActionResult();
    }

    private Guid? CurrentUserId =>
        Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId) ? userId : null;
}
