using System.ComponentModel;
using InstitutionalDiwan.Application.Common;
using InstitutionalDiwan.Application.Features.Identity;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>Minimal administrative picker reads; not full employee or role administration.</summary>
[ApiController]
[Route("api/identity-options")]
[Produces("application/json")]
[Authorize]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
public sealed class IdentityOptionsController(ISender sender, IAuthorizationService authorization) : ControllerBase
{
    [HttpGet("departments")]
    [EndpointSummary("Lists minimal department options for administrative and document forms.")]
    [EndpointDescription("Returns departmentId, code, nameAr and isActive, including inactive labels for existing references. Requires any one of USER_MANAGE, EMPLOYEE_MANAGE, DEPARTMENT_MANAGE, DOCUMENT_VIEW, DOCUMENT_CREATE or DOCUMENT_EDIT. Does not grant department administration. Responses: 200 (options), 401 (unauthenticated), 403 (no allowed permission).")]
    [ProducesResponseType<IReadOnlyCollection<DepartmentOptionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Departments(CancellationToken cancellationToken)
    {
        if (!await CanReadAsync(Permissions.UserManage, Permissions.EmployeeManage, Permissions.DepartmentManage,
                Permissions.DocumentView, Permissions.DocumentCreate, Permissions.DocumentEdit))
            return Forbid();
        return (await sender.Send(new ListDepartmentOptionsQuery(), cancellationToken)).ToActionResult();
    }

    [HttpGet("roles")]
    [EndpointSummary("Lists minimal role options for user role assignments.")]
    [EndpointDescription("Returns roleId, code, nameAr and isActive, including inactive roles so existing assignments can be revoked. Requires USER_MANAGE or ROLE_MANAGE. Does not expose role permissions or grant role administration. Responses: 200 (options), 401 (unauthenticated), 403 (no allowed permission).")]
    [ProducesResponseType<IReadOnlyCollection<RoleOptionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Roles(CancellationToken cancellationToken)
    {
        if (!await CanReadAsync(Permissions.UserManage, Permissions.RoleManage)) return Forbid();
        return (await sender.Send(new ListRoleOptionsQuery(), cancellationToken)).ToActionResult();
    }

    [HttpGet("employees")]
    [EndpointSummary("Searches a page of minimal employee options.")]
    [EndpointDescription("Returns employeeId, fullNameAr, employeeNumber and isActive in a paged envelope. Search matches names or employee number; includes inactive rows for display, not new selection. Requires USER_MANAGE, DEPARTMENT_MANAGE or EMPLOYEE_MANAGE. No contact details are returned. Responses: 200 (page), 401 (unauthenticated), 403 (no allowed permission).")]
    [ProducesResponseType<PagedResult<EmployeeOptionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Employees(
        [FromQuery, Description("One-based page number. Example: 2.")] int page = 1,
        [FromQuery, Description("Rows per page, clamped to 1-100. Example: 20.")] int pageSize = 20,
        [FromQuery, Description("Name or employee-number search term. Example: EMP-101.")] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (!await CanReadAsync(Permissions.UserManage, Permissions.DepartmentManage, Permissions.EmployeeManage))
            return Forbid();
        return (await sender.Send(new ListEmployeeOptionsQuery(page, pageSize, search), cancellationToken)).ToActionResult();
    }

    [HttpGet("employees/{employeeId:guid}")]
    [EndpointSummary("Resolves the minimal label of a selected employee.")]
    [EndpointDescription("Returns employeeId, fullNameAr, employeeNumber and isActive, even for an inactive existing selection. Requires USER_MANAGE, DEPARTMENT_MANAGE or EMPLOYEE_MANAGE; does not expose full employee details. Responses: 200 (option), 401 (unauthenticated), 403 (no allowed permission), 404 (employee not found).")]
    [ProducesResponseType<EmployeeOptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Employee(
        [Description("Selected employee identifier (GUID). Example: 3fa85f64-5717-4562-b3fc-2c963f66afa6.")] Guid employeeId,
        CancellationToken cancellationToken)
    {
        if (!await CanReadAsync(Permissions.UserManage, Permissions.DepartmentManage, Permissions.EmployeeManage))
            return Forbid();
        return (await sender.Send(new GetEmployeeOptionQuery(employeeId), cancellationToken)).ToActionResult();
    }

    [HttpGet("users")]
    [EndpointSummary("Searches a page of minimal user options for document access and routing.")]
    [EndpointDescription("Returns userId, username and isActive in a paged envelope. Search matches the username; inactive accounts are included so existing references can still be labelled. Requires USER_MANAGE, ACCESS_GRANT_MANAGE or DOCUMENT_EDIT. No contact details, roles, or department assignments are returned. Responses: 200 (page), 401 (unauthenticated), 403 (no allowed permission).")]
    [ProducesResponseType<PagedResult<UserOptionResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Users(
        [FromQuery, Description("One-based page number. Example: 2.")] int page = 1,
        [FromQuery, Description("Rows per page, clamped to 1-100. Example: 20.")] int pageSize = 20,
        [FromQuery, Description("Username search term. Example: ahmed.")] string? search = null,
        CancellationToken cancellationToken = default)
    {
        if (!await CanReadAsync(Permissions.UserManage, Permissions.AccessGrantManage, Permissions.DocumentEdit))
            return Forbid();
        return (await sender.Send(new ListUserOptionsQuery(page, pageSize, search), cancellationToken)).ToActionResult();
    }

    private async Task<bool> CanReadAsync(params string[] permissions)
    {
        if (User.Identity?.IsAuthenticated != true) return false;
        // OR existing exact permission policies, rather than broadening any management policy.
        foreach (var permission in permissions)
            if ((await authorization.AuthorizeAsync(User, null, permission)).Succeeded) return true;
        return false;
    }
}
