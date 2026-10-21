using InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Correspondence;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// External correspondents (ministries, companies, individuals). Reads require
/// <c>DOCUMENT_VIEW</c>; writes require <c>DOCUMENT_EDIT</c>.
/// </summary>
[ApiController]
[Route("api/external-organizations")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DocumentView)]
public sealed class ExternalOrganizationsController : ControllerBase
{
    private readonly ISender _sender;

    public ExternalOrganizationsController(ISender sender) => _sender = sender;

    /// <summary>Lists external organizations ordered by code.</summary>
    /// <param name="includeInactive">When true, deactivated organizations are included.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The external organization list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    [HttpGet]
    [EndpointSummary("Lists external organizations ordered by code.")]
    [EndpointDescription("Lists external organizations ordered by code. Requires the DOCUMENT_VIEW permission. Responses: 200 (The external organization list.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission.)")]
    [ProducesResponseType<IReadOnlyCollection<ExternalOrganizationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [Description("Include inactive records (boolean, query parameter). Example: true.")] [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ListExternalOrganizationsQuery(includeInactive), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Fetches an external organization by id.</summary>
    /// <param name="externalOrganizationId">External organization unique identifier (GUID, route). Example: <c>f6d9b4e0-5a83-4c27-8b4e-9f2c7d8a1e53</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The external organization.</response>
    /// <response code="404">No external organization with that id exists.</response>
    [HttpGet("{externalOrganizationId:guid}")]
    [EndpointSummary("Fetches an external organization by id.")]
    [EndpointDescription("Fetches an external organization by id. Requires the DOCUMENT_VIEW permission. Responses: 200 (The external organization.), 404 (No external organization with that id exists.)")]
    [ProducesResponseType<ExternalOrganizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [Description("External organization unique identifier (GUID, route). Example: f6d9b4e0-5a83-4c27-8b4e-9f2c7d8a1e53.")] Guid externalOrganizationId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetExternalOrganizationByIdQuery(externalOrganizationId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Registers a new external organization.</summary>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>code</term><description>Code (string, required). Example: <c>CODE-01</c>.</description></item>
    /// <item><term>nameAr</term><description>Name in Arabic (string, required). Example: <c>الإدارة المالية</c>.</description></item>
    /// <item><term>organizationTypeId</term><description>Organization type id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>nameEn</term><description>Name in English (string, optional). Example: <c>Finance Department</c>.</description></item>
    /// <item><term>contactInfo</term><description>Contact information (string, optional). Example: <c>info@example.org | +966112345678</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created external organization.</response>
    /// <response code="400">The payload failed validation or referenced an unknown organization type.</response>
    /// <response code="409">The code is already taken.</response>
    [HttpPost]
    [EndpointSummary("Registers a new external organization.")]
    [EndpointDescription("Registers a new external organization. Requires the DOCUMENT_EDIT permission. Responses: 200 (The created external organization.), 400 (The payload failed validation or referenced an unknown organization type.), 409 (The code is already taken.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<ExternalOrganizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateExternalOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateExternalOrganizationCommand(
            request.Code,
            request.NameAr,
            request.OrganizationTypeId,
            request.NameEn,
            request.ContactInfo), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Updates an external organization's descriptive fields (the code is immutable).</summary>
    /// <param name="externalOrganizationId">External organization unique identifier (GUID, route). Example: <c>f6d9b4e0-5a83-4c27-8b4e-9f2c7d8a1e53</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>nameAr</term><description>Name in Arabic (string, required). Example: <c>الإدارة المالية</c>.</description></item>
    /// <item><term>organizationTypeId</term><description>Organization type id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>nameEn</term><description>Name in English (string, optional). Example: <c>Finance Department</c>.</description></item>
    /// <item><term>contactInfo</term><description>Contact information (string, optional). Example: <c>info@example.org | +966112345678</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated external organization.</response>
    /// <response code="400">The payload failed validation or referenced an unknown organization type.</response>
    /// <response code="404">No external organization with that id exists.</response>
    [HttpPut("{externalOrganizationId:guid}")]
    [EndpointSummary("Updates an external organization's descriptive fields (the code is immutable).")]
    [EndpointDescription("Updates an external organization's descriptive fields (the code is immutable). Requires the DOCUMENT_EDIT permission. Responses: 200 (The updated external organization.), 400 (The payload failed validation or referenced an unknown organization type.), 404 (No external organization with that id exists.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<ExternalOrganizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [Description("External organization unique identifier (GUID, route). Example: f6d9b4e0-5a83-4c27-8b4e-9f2c7d8a1e53.")] Guid externalOrganizationId,
        [FromBody] UpdateExternalOrganizationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateExternalOrganizationCommand(
            externalOrganizationId,
            request.NameAr,
            request.OrganizationTypeId,
            request.NameEn,
            request.ContactInfo), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Deactivates an external organization (soft delete).</summary>
    /// <param name="externalOrganizationId">External organization unique identifier (GUID, route). Example: <c>f6d9b4e0-5a83-4c27-8b4e-9f2c7d8a1e53</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The deactivated external organization.</response>
    /// <response code="404">No external organization with that id exists.</response>
    [HttpDelete("{externalOrganizationId:guid}")]
    [EndpointSummary("Deactivates an external organization (soft delete).")]
    [EndpointDescription("Deactivates an external organization (soft delete). Requires the DOCUMENT_EDIT permission. Responses: 200 (The deactivated external organization.), 404 (No external organization with that id exists.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<ExternalOrganizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate([Description("External organization unique identifier (GUID, route). Example: f6d9b4e0-5a83-4c27-8b4e-9f2c7d8a1e53.")] Guid externalOrganizationId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeactivateExternalOrganizationCommand(externalOrganizationId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated external organization.</summary>
    /// <param name="externalOrganizationId">External organization unique identifier (GUID, route). Example: <c>f6d9b4e0-5a83-4c27-8b4e-9f2c7d8a1e53</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The reactivated external organization.</response>
    /// <response code="404">No external organization with that id exists.</response>
    [HttpPost("{externalOrganizationId:guid}/activate")]
    [EndpointSummary("Reactivates a previously deactivated external organization.")]
    [EndpointDescription("Reactivates a previously deactivated external organization. Requires the DOCUMENT_EDIT permission. Responses: 200 (The reactivated external organization.), 404 (No external organization with that id exists.)")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<ExternalOrganizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate([Description("External organization unique identifier (GUID, route). Example: f6d9b4e0-5a83-4c27-8b4e-9f2c7d8a1e53.")] Guid externalOrganizationId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ActivateExternalOrganizationCommand(externalOrganizationId), cancellationToken);

        return result.ToActionResult();
    }
}
