using InstitutionalDiwan.Application.Features.Organization.Departments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Organization;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

[ApiController]
[Route("api/departments")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DepartmentManage)]
public sealed class DepartmentsController : ControllerBase
{
    private readonly ISender _sender;

    public DepartmentsController(ISender sender) => _sender = sender;

    /// <summary>Lists every department as a flat collection, ordered by code.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The department list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DEPARTMENT_MANAGE permission.</response>
    [HttpGet]
    [EndpointSummary("Lists every department as a flat collection, ordered by code.")]
    [EndpointDescription("Lists every department as a flat collection, ordered by code. Requires the DEPARTMENT_MANAGE permission. Responses: 200 (The department list.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DEPARTMENT_MANAGE permission.)")]
    [ProducesResponseType<IReadOnlyCollection<DepartmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListDepartmentsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Returns the department hierarchy as a collection of root nodes.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The department tree.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DEPARTMENT_MANAGE permission.</response>
    [HttpGet("tree")]
    [EndpointSummary("Returns the department hierarchy as a collection of root nodes.")]
    [EndpointDescription("Returns the department hierarchy as a collection of root nodes. Requires the DEPARTMENT_MANAGE permission. Responses: 200 (The department tree.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DEPARTMENT_MANAGE permission.)")]
    [ProducesResponseType<IReadOnlyCollection<DepartmentTreeNodeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Tree(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDepartmentTreeQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches a department by id.</summary>
    /// <param name="departmentId">Department unique identifier (GUID, route). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The department.</response>
    /// <response code="404">No department with that id exists.</response>
    [HttpGet("{departmentId:guid}")]
    [EndpointSummary("Fetches a department by id.")]
    [EndpointDescription("Fetches a department by id. Requires the DEPARTMENT_MANAGE permission. Responses: 200 (The department.), 404 (No department with that id exists.)")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([Description("Department unique identifier (GUID, route). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid departmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDepartmentByIdQuery(departmentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a new department under an optional parent.</summary>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>code</term><description>Code (string, required). Example: <c>CODE-01</c>.</description></item>
    /// <item><term>nameAr</term><description>Name in Arabic (string, required). Example: <c>الإدارة المالية</c>.</description></item>
    /// <item><term>departmentTypeId</term><description>Department type id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>parentDepartmentId</term><description>Parent department unique identifier (GUID, optional). Example: <c>8b4d1f70-6c93-42a5-b18e-4d7a3c5f9e12</c>.</description></item>
    /// <item><term>nameEn</term><description>Name in English (string, optional). Example: <c>Finance Department</c>.</description></item>
    /// <item><term>canSendExternal</term><description>Can send external (boolean, optional, default <c>false</c>). Example: <c>true</c>.</description></item>
    /// <item><term>canReceiveExternal</term><description>Can receive external (boolean, optional, default <c>false</c>). Example: <c>true</c>.</description></item>
    /// <item><term>notes</term><description>Notes (string, optional). Example: <c>Reviewed and forwarded to the responsible department.</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created department.</response>
    /// <response code="400">The payload failed validation or referenced an unknown/inactive department type or parent.</response>
    /// <response code="409">The department code is already taken, or the department type is inactive.</response>
    [HttpPost]
    [EndpointSummary("Creates a new department under an optional parent.")]
    [EndpointDescription("Creates a new department under an optional parent. Requires the DEPARTMENT_MANAGE permission. Responses: 200 (The created department.), 400 (The payload failed validation or referenced an unknown/inactive department type or parent.), 409 (The department code is already taken, or the department type is inactive.)")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateDepartmentCommand(
            request.Code,
            request.NameAr,
            request.DepartmentTypeId,
            request.ParentDepartmentId,
            request.NameEn,
            request.CanSendExternal,
            request.CanReceiveExternal,
            request.Notes), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Updates a department's name, parent, and external-correspondence policy.</summary>
    /// <param name="departmentId">Department unique identifier (GUID, route). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>nameAr</term><description>Name in Arabic (string, required). Example: <c>الإدارة المالية</c>.</description></item>
    /// <item><term>parentDepartmentId</term><description>Parent department unique identifier (GUID, optional). Example: <c>8b4d1f70-6c93-42a5-b18e-4d7a3c5f9e12</c>.</description></item>
    /// <item><term>canSendExternal</term><description>Can send external (boolean, optional, default <c>false</c>). Example: <c>true</c>.</description></item>
    /// <item><term>canReceiveExternal</term><description>Can receive external (boolean, optional, default <c>false</c>). Example: <c>true</c>.</description></item>
    /// <item><term>nameEn</term><description>Name in English (string, optional). Example: <c>Finance Department</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated department.</response>
    /// <response code="400">The payload failed validation or referenced an unknown parent.</response>
    /// <response code="404">No department with that id exists.</response>
    /// <response code="409">The move would create a cycle or target an inactive parent.</response>
    [HttpPut("{departmentId:guid}")]
    [EndpointSummary("Updates a department's name, parent, and external-correspondence policy.")]
    [EndpointDescription("Updates a department's name, parent, and external-correspondence policy. Requires the DEPARTMENT_MANAGE permission. Responses: 200 (The updated department.), 400 (The payload failed validation or referenced an unknown parent.), 404 (No department with that id exists.), 409 (The move would create a cycle or target an inactive parent.)")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [Description("Department unique identifier (GUID, route). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid departmentId,
        [FromBody] UpdateDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateDepartmentCommand(
            departmentId,
            request.NameAr,
            request.ParentDepartmentId,
            request.CanSendExternal,
            request.CanReceiveExternal,
            request.NameEn), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Reparents a department. The move is rejected with 409 when the new parent is the department
    /// itself, one of its own descendants (a hierarchy cycle), or an inactive department; a missing
    /// new parent is reported as 400 and a missing department as 404. A <c>null</c> parent promotes
    /// the department to a root.
    /// </summary>
    /// <param name="departmentId">Department unique identifier (GUID, route). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>parentDepartmentId</term><description>Parent department unique identifier (GUID, optional). Example: <c>8b4d1f70-6c93-42a5-b18e-4d7a3c5f9e12</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The moved department.</response>
    /// <response code="400">The new parent department does not exist.</response>
    /// <response code="404">No department with that id exists.</response>
    /// <response code="409">The move would create a cycle, target the department itself, or target an inactive parent.</response>
    [HttpPut("{departmentId:guid}/parent")]
    [EndpointSummary("Reparents a department. The move is rejected with 409 when the new parent is the department itself, one of its own descendants (a hierarchy cycle), or an inactive department; a missing new parent is reported as 400 and a missing department as 404. A null parent promotes the department to a root.")]
    [EndpointDescription("Reparents a department. The move is rejected with 409 when the new parent is the department itself, one of its own descendants (a hierarchy cycle), or an inactive department; a missing new parent is reported as 400 and a missing department as 404. A null parent promotes the department to a root. Requires the DEPARTMENT_MANAGE permission. Responses: 200 (The moved department.), 400 (The new parent department does not exist.), 404 (No department with that id exists.), 409 (The move would create a cycle, target the department itself, or target an inactive parent.)")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> MoveToParent(
        [Description("Department unique identifier (GUID, route). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid departmentId,
        [FromBody] MoveDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new MoveDepartmentCommand(departmentId, request.ParentDepartmentId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>
    /// Soft-deletes a department by deactivating it. Fails with 409 when it still has active children.
    /// </summary>
    /// <param name="departmentId">Department unique identifier (GUID, route). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The department was deactivated.</response>
    /// <response code="404">No department with that id exists.</response>
    /// <response code="409">The department still has active child departments.</response>
    [HttpDelete("{departmentId:guid}")]
    [EndpointSummary("Soft-deletes a department by deactivating it. Fails with 409 when it still has active children.")]
    [EndpointDescription("Soft-deletes a department by deactivating it. Fails with 409 when it still has active children. Requires the DEPARTMENT_MANAGE permission. Responses: 200 (The department was deactivated.), 404 (No department with that id exists.), 409 (The department still has active child departments.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([Description("Department unique identifier (GUID, route). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid departmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteDepartmentCommand(departmentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated department.</summary>
    /// <param name="departmentId">Department unique identifier (GUID, route). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The reactivated department.</response>
    /// <response code="404">No department with that id exists.</response>
    [HttpPost("{departmentId:guid}/activate")]
    [EndpointSummary("Reactivates a previously deactivated department.")]
    [EndpointDescription("Reactivates a previously deactivated department. Requires the DEPARTMENT_MANAGE permission. Responses: 200 (The reactivated department.), 404 (No department with that id exists.)")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate([Description("Department unique identifier (GUID, route). Example: e7a62f43-9b82-4d01-8c2e-7ab91e506d42.")] Guid departmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateDepartmentCommand(departmentId), cancellationToken);
        return result.ToActionResult();
    }
}
