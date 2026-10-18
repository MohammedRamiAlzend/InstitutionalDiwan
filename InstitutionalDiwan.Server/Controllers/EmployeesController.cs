using InstitutionalDiwan.Application.Features.Identity.Employees;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Identity;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

[ApiController]
[Route("api/employees")]
[Produces("application/json")]
[Authorize(Policy = Permissions.EmployeeManage)]
public sealed class EmployeesController : ControllerBase
{
    private readonly ISender _sender;

    public EmployeesController(ISender sender) => _sender = sender;

    /// <summary>Lists all employee records. Requires the <c>EMPLOYEE_MANAGE</c> permission.</summary>
    /// <response code="200">The employee list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token does not carry the EMPLOYEE_MANAGE permission.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<EmployeeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListEmployeesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches an employee by id.</summary>
    /// <response code="200">The employee.</response>
    /// <response code="404">No employee with that id exists.</response>
    [HttpGet("{employeeId:guid}")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetEmployeeByIdQuery(employeeId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a new employee record attached to a department.</summary>
    /// <response code="200">The created employee.</response>
    /// <response code="400">The payload failed validation or referenced an unknown department.</response>
    [HttpPost]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create(
        [FromBody] CreateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateEmployeeCommand(
            request.FullNameAr,
            request.DepartmentId,
            request.EmployeeNumber,
            request.FullNameEn,
            request.JobTitleAr,
            request.Email,
            request.Phone), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Updates an employee's details and department.</summary>
    /// <response code="200">The updated employee.</response>
    /// <response code="400">The payload failed validation or referenced an unknown department.</response>
    /// <response code="404">No employee with that id exists.</response>
    [HttpPut("{employeeId:guid}")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid employeeId,
        [FromBody] UpdateEmployeeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateEmployeeCommand(
            employeeId,
            request.FullNameAr,
            request.DepartmentId,
            request.EmployeeNumber,
            request.FullNameEn,
            request.JobTitleAr,
            request.Email,
            request.Phone), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Soft-deletes an employee by deactivating the record.</summary>
    /// <response code="200">The deactivated employee.</response>
    /// <response code="404">No employee with that id exists.</response>
    [HttpDelete("{employeeId:guid}")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateEmployeeCommand(employeeId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated employee.</summary>
    /// <response code="200">The reactivated employee.</response>
    /// <response code="404">No employee with that id exists.</response>
    [HttpPost("{employeeId:guid}/activate")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateEmployeeCommand(employeeId), cancellationToken);
        return result.ToActionResult();
    }
}
