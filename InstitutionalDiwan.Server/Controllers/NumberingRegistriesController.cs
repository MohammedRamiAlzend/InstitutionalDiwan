using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Numbering;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

[ApiController]
[Route("api/numbering/registries")]
[Produces("application/json")]
[Authorize(Policy = Permissions.NumberingView)]
public sealed class NumberingRegistriesController : ControllerBase
{
    private readonly ISender _sender;

    public NumberingRegistriesController(ISender sender) => _sender = sender;

    /// <summary>Lists every numbering registry with its id, scope, and counters.</summary>
    /// <response code="200">The numbering registry list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the NUMBERING_VIEW permission.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyCollection<NumberingRegistryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListRegistriesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches a single numbering registry by its id.</summary>
    /// <response code="200">The numbering registry.</response>
    /// <response code="404">No numbering registry with that id exists.</response>
    [HttpGet("{registryId:guid}")]
    [ProducesResponseType<NumberingRegistryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid registryId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRegistryByIdQuery(registryId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a numbering registry.</summary>
    /// <response code="200">The created registry.</response>
    /// <response code="400">The payload failed validation or referenced an unknown scope/policy.</response>
    /// <response code="409">The registry code is already taken.</response>
    [HttpPost]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<NumberingRegistryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateNumberingRegistryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateNumberingRegistryCommand(
            request.Code,
            request.NameAr,
            request.FormatPattern,
            request.ResetPolicyId,
            request.DepartmentId,
            request.DocumentTypeId,
            request.DirectionId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Updates a registry's name, scope, reset policy, and format template.</summary>
    /// <response code="200">The updated registry.</response>
    /// <response code="400">The payload failed validation or referenced an unknown scope/policy.</response>
    /// <response code="404">No registry with that id exists.</response>
    /// <response code="409">The format cannot change because numbers were already issued from it.</response>
    [HttpPut("{registryId:guid}")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<NumberingRegistryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid registryId,
        [FromBody] UpdateNumberingRegistryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateNumberingRegistryCommand(
            registryId,
            request.NameAr,
            request.FormatPattern,
            request.ResetPolicyId,
            request.DepartmentId,
            request.DocumentTypeId,
            request.DirectionId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Deactivates a registry so it is no longer selected for new documents.</summary>
    /// <response code="200">The deactivated registry.</response>
    /// <response code="404">No registry with that id exists.</response>
    [HttpDelete("{registryId:guid}")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<NumberingRegistryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid registryId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeactivateNumberingRegistryCommand(registryId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated registry.</summary>
    /// <response code="200">The reactivated registry.</response>
    /// <response code="404">No registry with that id exists.</response>
    [HttpPost("{registryId:guid}/activate")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<NumberingRegistryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate(Guid registryId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ActivateNumberingRegistryCommand(registryId), cancellationToken);

        return result.ToActionResult();
    }
}
