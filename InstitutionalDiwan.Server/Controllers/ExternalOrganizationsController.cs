using InstitutionalDiwan.Application.Features.Correspondence.ExternalOrganizations;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Correspondence;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    /// <response code="200">The external organization list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<ExternalOrganizationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ListExternalOrganizationsQuery(includeInactive), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Fetches an external organization by id.</summary>
    /// <response code="200">The external organization.</response>
    /// <response code="404">No external organization with that id exists.</response>
    [HttpGet("{externalOrganizationId:guid}")]
    [ProducesResponseType<ExternalOrganizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid externalOrganizationId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new GetExternalOrganizationByIdQuery(externalOrganizationId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Registers a new external organization.</summary>
    /// <response code="200">The created external organization.</response>
    /// <response code="400">The payload failed validation or referenced an unknown organization type.</response>
    /// <response code="409">The code is already taken.</response>
    [HttpPost]
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
    /// <response code="200">The updated external organization.</response>
    /// <response code="400">The payload failed validation or referenced an unknown organization type.</response>
    /// <response code="404">No external organization with that id exists.</response>
    [HttpPut("{externalOrganizationId:guid}")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<ExternalOrganizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid externalOrganizationId,
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
    /// <response code="200">The deactivated external organization.</response>
    /// <response code="404">No external organization with that id exists.</response>
    [HttpDelete("{externalOrganizationId:guid}")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<ExternalOrganizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid externalOrganizationId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeactivateExternalOrganizationCommand(externalOrganizationId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated external organization.</summary>
    /// <response code="200">The reactivated external organization.</response>
    /// <response code="404">No external organization with that id exists.</response>
    [HttpPost("{externalOrganizationId:guid}/activate")]
    [Authorize(Policy = Permissions.DocumentEdit)]
    [ProducesResponseType<ExternalOrganizationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid externalOrganizationId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ActivateExternalOrganizationCommand(externalOrganizationId), cancellationToken);

        return result.ToActionResult();
    }
}
