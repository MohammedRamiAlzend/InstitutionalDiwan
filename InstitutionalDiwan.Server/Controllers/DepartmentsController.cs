using InstitutionalDiwan.Application.Features.Organization.Departments;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Organization;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// <response code="200">The department list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DEPARTMENT_MANAGE permission.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<DepartmentResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListDepartmentsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Returns the department hierarchy as a collection of root nodes.</summary>
    /// <response code="200">The department tree.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DEPARTMENT_MANAGE permission.</response>
    [HttpGet("tree")]
    [ProducesResponseType<IReadOnlyCollection<DepartmentTreeNodeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Tree(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDepartmentTreeQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches a department by id.</summary>
    /// <response code="200">The department.</response>
    /// <response code="404">No department with that id exists.</response>
    [HttpGet("{departmentId:guid}")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid departmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetDepartmentByIdQuery(departmentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a new department under an optional parent.</summary>
    /// <response code="200">The created department.</response>
    /// <response code="400">The payload failed validation or referenced an unknown/inactive department type or parent.</response>
    /// <response code="409">The department code is already taken, or the department type is inactive.</response>
    [HttpPost]
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
            request.DepartmentTypeCode,
            request.ParentDepartmentId,
            request.NameEn,
            request.CanSendExternal,
            request.CanReceiveExternal,
            request.Notes), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Updates a department's name, parent, and external-correspondence policy.</summary>
    /// <response code="200">The updated department.</response>
    /// <response code="400">The payload failed validation or referenced an unknown parent.</response>
    /// <response code="404">No department with that id exists.</response>
    /// <response code="409">The move would create a cycle or target an inactive parent.</response>
    [HttpPut("{departmentId:guid}")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid departmentId,
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
    /// Soft-deletes a department by deactivating it. Fails with 409 when it still has active children.
    /// </summary>
    /// <response code="200">The department was deactivated.</response>
    /// <response code="404">No department with that id exists.</response>
    /// <response code="409">The department still has active child departments.</response>
    [HttpDelete("{departmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid departmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeleteDepartmentCommand(departmentId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated department.</summary>
    /// <response code="200">The reactivated department.</response>
    /// <response code="404">No department with that id exists.</response>
    [HttpPost("{departmentId:guid}/activate")]
    [ProducesResponseType<DepartmentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid departmentId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateDepartmentCommand(departmentId), cancellationToken);
        return result.ToActionResult();
    }
}
