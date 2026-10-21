using InstitutionalDiwan.Application.Features.Identity.Employees;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Identity;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

[ApiController]
[Route("api/employees")]
[Produces("application/json")]
[Authorize(Policy = Permissions.EmployeeManage)]
public sealed class EmployeesController : ControllerBase
{
    private readonly ISender _sender;

    public EmployeesController(ISender sender) => _sender = sender;

    /// <summary>Lists all employee records.</summary>
    /// <remarks>
    /// Returns all employee records in the system. Requires the <c>EMPLOYEE_MANAGE</c> permission.
    /// </remarks>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The employee list returned as <c>IReadOnlyCollection&lt;EmployeeResponse&gt;</c>.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token does not carry the EMPLOYEE_MANAGE permission.</response>
    [HttpGet]
    [EndpointSummary("Lists all employee records.")]
    [EndpointDescription("Lists all employee records. Returns all employee records in the system. Requires the EMPLOYEE_MANAGE permission. Responses: 200 (The employee list returned as IReadOnlyCollection<EmployeeResponse>.), 401 (The token is missing, expired, or invalid.), 403 (The token does not carry the EMPLOYEE_MANAGE permission.)")]
    [Produces("application/json", Type = typeof(IReadOnlyCollection<EmployeeResponse>))]
    [ProducesResponseType<IReadOnlyCollection<EmployeeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListEmployeesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches an employee by id.</summary>
    /// <remarks>
    /// Retrieves a specific employee record by its unique identifier.
    /// </remarks>
    /// <param name="employeeId">The unique identifier of the employee (GUID). Example: <c>3fa85f64-5717-4562-b3fc-2c963f66afa6</c></param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The employee returned as <c>EmployeeResponse</c>.</response>
    /// <response code="404">No employee with that id exists.</response>
    [HttpGet("{employeeId:guid}")]
    [EndpointSummary("Fetches an employee by id.")]
    [EndpointDescription("Fetches an employee by id. Retrieves a specific employee record by its unique identifier. Requires the EMPLOYEE_MANAGE permission. Responses: 200 (The employee returned as EmployeeResponse.), 404 (No employee with that id exists.)")]
    [Produces("application/json", Type = typeof(EmployeeResponse))]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([Description("Employee unique identifier (GUID). Example: 3fa85f64-5717-4562-b3fc-2c963f66afa6")] Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetEmployeeByIdQuery(employeeId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a new employee record attached to a department.</summary>
    /// <remarks>
    /// Creates a new employee record and associates it with the specified department.
    /// Requires the <c>EMPLOYEE_MANAGE</c> permission.
    /// </remarks>
    /// <param name="request">
    /// Employee creation payload.
    /// <list type="bullet">
    /// <item><term>fullNameAr</term><description>Full name in Arabic (string, required). Example: <c>أحمد محمد علي</c></description></item>
    /// <item><term>departmentId</term><description>Department GUID (required). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c></description></item>
    /// <item><term>employeeNumber</term><description>Employee number (string, optional). Example: <c>EMP-2024-001</c></description></item>
    /// <item><term>fullNameEn</term><description>Full name in English (string, optional). Example: <c>Ahmed Mohammed Ali</c></description></item>
    /// <item><term>jobTitleAr</term><description>Job title in Arabic (string, optional). Example: <c>مطور برمجيات</c></description></item>
    /// <item><term>email</term><description>Email address (string, optional). Example: <c>ahmed.ali@example.com</c></description></item>
    /// <item><term>phone</term><description>Phone number (string, optional). Example: <c>+966501234567</c></description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created employee returned as <c>EmployeeResponse</c>.</response>
    /// <response code="400">The payload failed validation or referenced an unknown department.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token does not carry the EMPLOYEE_MANAGE permission.</response>
    [HttpPost]
    [EndpointSummary("Creates a new employee record attached to a department.")]
    [EndpointDescription("Creates a new employee record attached to a department. Creates a new employee record and associates it with the specified department. Requires the EMPLOYEE_MANAGE permission. Responses: 200 (The created employee returned as EmployeeResponse.), 400 (The payload failed validation or referenced an unknown department.), 401 (The token is missing, expired, or invalid.), 403 (The token does not carry the EMPLOYEE_MANAGE permission.)")]
    [Produces("application/json", Type = typeof(EmployeeResponse))]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody, Description("Employee creation details.")] CreateEmployeeRequest request,
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
    /// <param name="employeeId">Employee unique identifier (GUID, route). Example: <c>3fa85f64-5717-4562-b3fc-2c963f66afa6</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>fullNameAr</term><description>Full name in Arabic (string, required). Example: <c>أحمد محمد علي</c>.</description></item>
    /// <item><term>departmentId</term><description>Department unique identifier (GUID, required). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</description></item>
    /// <item><term>employeeNumber</term><description>Employee number (string, optional). Example: <c>EMP-2024-001</c>.</description></item>
    /// <item><term>fullNameEn</term><description>Full name in English (string, optional). Example: <c>Ahmed Mohammed Ali</c>.</description></item>
    /// <item><term>jobTitleAr</term><description>Job title in Arabic (string, optional). Example: <c>مطور برمجيات</c>.</description></item>
    /// <item><term>email</term><description>Email address (string, optional). Example: <c>ahmed.ali@example.com</c>.</description></item>
    /// <item><term>phone</term><description>Phone number (string, optional). Example: <c>+966501234567</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated employee.</response>
    /// <response code="400">The payload failed validation or referenced an unknown department.</response>
    /// <response code="404">No employee with that id exists.</response>
    [HttpPut("{employeeId:guid}")]
    [EndpointSummary("Updates an employee's details and department.")]
    [EndpointDescription("Updates an employee's details and department. Requires the EMPLOYEE_MANAGE permission. Responses: 200 (The updated employee.), 400 (The payload failed validation or referenced an unknown department.), 404 (No employee with that id exists.)")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [Description("Employee unique identifier (GUID, route). Example: 3fa85f64-5717-4562-b3fc-2c963f66afa6.")] Guid employeeId,
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
    /// <param name="employeeId">Employee unique identifier (GUID, route). Example: <c>3fa85f64-5717-4562-b3fc-2c963f66afa6</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The deactivated employee.</response>
    /// <response code="404">No employee with that id exists.</response>
    [HttpDelete("{employeeId:guid}")]
    [EndpointSummary("Soft-deletes an employee by deactivating the record.")]
    [EndpointDescription("Soft-deletes an employee by deactivating the record. Requires the EMPLOYEE_MANAGE permission. Responses: 200 (The deactivated employee.), 404 (No employee with that id exists.)")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([Description("Employee unique identifier (GUID, route). Example: 3fa85f64-5717-4562-b3fc-2c963f66afa6.")] Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateEmployeeCommand(employeeId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated employee.</summary>
    /// <param name="employeeId">Employee unique identifier (GUID, route). Example: <c>3fa85f64-5717-4562-b3fc-2c963f66afa6</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The reactivated employee.</response>
    /// <response code="404">No employee with that id exists.</response>
    [HttpPost("{employeeId:guid}/activate")]
    [EndpointSummary("Reactivates a previously deactivated employee.")]
    [EndpointDescription("Reactivates a previously deactivated employee. Requires the EMPLOYEE_MANAGE permission. Responses: 200 (The reactivated employee.), 404 (No employee with that id exists.)")]
    [ProducesResponseType<EmployeeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate([Description("Employee unique identifier (GUID, route). Example: 3fa85f64-5717-4562-b3fc-2c963f66afa6.")] Guid employeeId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateEmployeeCommand(employeeId), cancellationToken);
        return result.ToActionResult();
    }
}
