using InstitutionalDiwan.Application.Features.Numbering.Registries;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Numbering;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

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
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The numbering registry list.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the NUMBERING_VIEW permission.</response>
    [HttpGet]
    [EndpointSummary("Lists every numbering registry with its id, scope, and counters.")]
    [EndpointDescription("Lists every numbering registry with its id, scope, and counters. Requires the NUMBERING_VIEW permission. Responses: 200 (The numbering registry list.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the NUMBERING_VIEW permission.)")]
    [ProducesResponseType<IReadOnlyCollection<NumberingRegistryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ListRegistriesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Fetches a single numbering registry by its id.</summary>
    /// <param name="registryId">Registry unique identifier (GUID, route). Example: <c>b2f4c8d1-3a6e-4f7b-9c05-8d1e2a6f4b33</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The numbering registry.</response>
    /// <response code="404">No numbering registry with that id exists.</response>
    [HttpGet("{registryId:guid}")]
    [EndpointSummary("Fetches a single numbering registry by its id.")]
    [EndpointDescription("Fetches a single numbering registry by its id. Requires the NUMBERING_VIEW permission. Responses: 200 (The numbering registry.), 404 (No numbering registry with that id exists.)")]
    [ProducesResponseType<NumberingRegistryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([Description("Registry unique identifier (GUID, route). Example: b2f4c8d1-3a6e-4f7b-9c05-8d1e2a6f4b33.")] Guid registryId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetRegistryByIdQuery(registryId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a numbering registry.</summary>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>code</term><description>Code (string, required). Example: <c>CODE-01</c>.</description></item>
    /// <item><term>nameAr</term><description>Name in Arabic (string, required). Example: <c>الإدارة المالية</c>.</description></item>
    /// <item><term>formatPattern</term><description>Format pattern (string, optional). Example: <c>DOC-{YEAR}-{SEQ:0000}</c>.</description></item>
    /// <item><term>resetPolicyId</term><description>Reset policy id (integer, optional, default <c>0</c>). Example: <c>1</c>.</description></item>
    /// <item><term>departmentId</term><description>Department unique identifier (GUID, optional). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</description></item>
    /// <item><term>documentTypeId</term><description>Document type id (integer, optional). Example: <c>1</c>.</description></item>
    /// <item><term>directionId</term><description>Direction id (integer, optional). Example: <c>1</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created registry.</response>
    /// <response code="400">The payload failed validation or referenced an unknown scope/policy.</response>
    /// <response code="409">The registry code is already taken.</response>
    [HttpPost]
    [EndpointSummary("Creates a numbering registry.")]
    [EndpointDescription("Creates a numbering registry. Requires the NUMBERING_MANAGE permission. Responses: 200 (The created registry.), 400 (The payload failed validation or referenced an unknown scope/policy.), 409 (The registry code is already taken.)")]
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
    /// <param name="registryId">Registry unique identifier (GUID, route). Example: <c>b2f4c8d1-3a6e-4f7b-9c05-8d1e2a6f4b33</c>.</param>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>nameAr</term><description>Name in Arabic (string, required). Example: <c>الإدارة المالية</c>.</description></item>
    /// <item><term>formatPattern</term><description>Format pattern (string, optional). Example: <c>DOC-{YEAR}-{SEQ:0000}</c>.</description></item>
    /// <item><term>resetPolicyId</term><description>Reset policy id (integer, optional, default <c>0</c>). Example: <c>1</c>.</description></item>
    /// <item><term>departmentId</term><description>Department unique identifier (GUID, optional). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</description></item>
    /// <item><term>documentTypeId</term><description>Document type id (integer, optional). Example: <c>1</c>.</description></item>
    /// <item><term>directionId</term><description>Direction id (integer, optional). Example: <c>1</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated registry.</response>
    /// <response code="400">The payload failed validation or referenced an unknown scope/policy.</response>
    /// <response code="404">No registry with that id exists.</response>
    /// <response code="409">The format cannot change because numbers were already issued from it.</response>
    [HttpPut("{registryId:guid}")]
    [EndpointSummary("Updates a registry's name, scope, reset policy, and format template.")]
    [EndpointDescription("Updates a registry's name, scope, reset policy, and format template. Requires the NUMBERING_MANAGE permission. Responses: 200 (The updated registry.), 400 (The payload failed validation or referenced an unknown scope/policy.), 404 (No registry with that id exists.), 409 (The format cannot change because numbers were already issued from it.)")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<NumberingRegistryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [Description("Registry unique identifier (GUID, route). Example: b2f4c8d1-3a6e-4f7b-9c05-8d1e2a6f4b33.")] Guid registryId,
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
    /// <param name="registryId">Registry unique identifier (GUID, route). Example: <c>b2f4c8d1-3a6e-4f7b-9c05-8d1e2a6f4b33</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The deactivated registry.</response>
    /// <response code="404">No registry with that id exists.</response>
    [HttpDelete("{registryId:guid}")]
    [EndpointSummary("Deactivates a registry so it is no longer selected for new documents.")]
    [EndpointDescription("Deactivates a registry so it is no longer selected for new documents. Requires the NUMBERING_MANAGE permission. Responses: 200 (The deactivated registry.), 404 (No registry with that id exists.)")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<NumberingRegistryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate([Description("Registry unique identifier (GUID, route). Example: b2f4c8d1-3a6e-4f7b-9c05-8d1e2a6f4b33.")] Guid registryId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new DeactivateNumberingRegistryCommand(registryId), cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Reactivates a previously deactivated registry.</summary>
    /// <param name="registryId">Registry unique identifier (GUID, route). Example: <c>b2f4c8d1-3a6e-4f7b-9c05-8d1e2a6f4b33</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The reactivated registry.</response>
    /// <response code="404">No registry with that id exists.</response>
    [HttpPost("{registryId:guid}/activate")]
    [EndpointSummary("Reactivates a previously deactivated registry.")]
    [EndpointDescription("Reactivates a previously deactivated registry. Requires the NUMBERING_MANAGE permission. Responses: 200 (The reactivated registry.), 404 (No registry with that id exists.)")]
    [Authorize(Policy = Permissions.NumberingManage)]
    [ProducesResponseType<NumberingRegistryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Activate([Description("Registry unique identifier (GUID, route). Example: b2f4c8d1-3a6e-4f7b-9c05-8d1e2a6f4b33.")] Guid registryId, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new ActivateNumberingRegistryCommand(registryId), cancellationToken);

        return result.ToActionResult();
    }
}
