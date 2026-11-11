using InstitutionalDiwan.Application.Features.Decisions;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Decisions;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>Administrator-managed decision types and the document types they apply to.</summary>
[ApiController]
[Route("api/decision-types")]
[Produces("application/json")]
[Authorize]
public sealed class DecisionTypesController : ControllerBase
{
    private readonly ISender _sender;

    public DecisionTypesController(ISender sender) => _sender = sender;

    /// <summary>Lists decision types, optionally for one document type and one active state.</summary>
    /// <param name="documentTypeId">Document type id (integer, optional, query). Example: <c>1</c>.</param>
    /// <param name="isActive">Keep only active or only inactive types (boolean, optional, query). Example: <c>true</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The matching decision types.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DOCUMENT_VIEW permission.</response>
    [HttpGet]
    [Authorize(Policy = Permissions.DocumentView)]
    [EndpointSummary("Lists decision types, optionally for one document type and one active state.")]
    [EndpointDescription("Lists decision types, optionally for one document type and one active state. Requires the DOCUMENT_VIEW permission. Responses: 200 (The matching decision types.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DOCUMENT_VIEW permission.)")]
    [ProducesResponseType<IReadOnlyList<DecisionTypeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [Description("Document type id (integer, optional, query). Example: 1.")] [FromQuery] int? documentTypeId = null,
        [Description("Keep only active or only inactive types (boolean, optional, query). Example: true.")] [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(new ListDecisionTypesQuery(documentTypeId, isActive), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Creates a decision type. A referral without a destination is stored inactive.</summary>
    /// <param name="request">
    /// The request payload:
    /// <list type="bullet">
    /// <item><term>nameAr</term><description>Arabic name (string, required, at most 200). Example: <c>منع المحاكمة</c>.</description></item>
    /// <item><term>isAppealable</term><description>Whether a decision of this type can be appealed (boolean, required). Example: <c>false</c>.</description></item>
    /// <item><term>effectId</term><description>Effect: 1 refer, 2 send to archive, 3 join, 4 none (integer, required). Example: <c>2</c>.</description></item>
    /// <item><term>destinationDepartmentId</term><description>Destination department, required to activate a referral and forbidden for every other effect (GUID, optional). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</description></item>
    /// <item><term>documentTypeIds</term><description>Document types this decision type applies to (integer array, required). Example: <c>[1]</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The created decision type.</response>
    /// <response code="400">The payload failed validation.</response>
    /// <response code="403">The token lacks the LOOKUP_MANAGE permission.</response>
    /// <response code="409">The effect, destination, or document-type links are not allowed.</response>
    [HttpPost]
    [Authorize(Policy = Permissions.LookupManage)]
    [EndpointSummary("Creates a decision type. A referral without a destination is stored inactive.")]
    [EndpointDescription("Creates a decision type. A referral without a destination is stored inactive. Requires the LOOKUP_MANAGE permission. Responses: 200 (The created decision type.), 400 (The payload failed validation.), 403 (The token lacks the LOOKUP_MANAGE permission.), 409 (The effect, destination, or document-type links are not allowed.)")]
    [ProducesResponseType<DecisionTypeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDecisionTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new CreateDecisionTypeCommand(
            request.NameAr,
            request.IsAppealable,
            request.EffectId,
            request.DestinationDepartmentId,
            request.DocumentTypeIds), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Updates a decision type. Issued decisions keep the snapshot they were given.</summary>
    /// <param name="decisionTypeId">Decision type id (GUID, route). Example: <c>a1000004-0000-4000-8000-000000000004</c>.</param>
    /// <param name="request">
    /// The request payload:
    /// <list type="bullet">
    /// <item><term>nameAr</term><description>Arabic name (string, required, at most 200). Example: <c>منع المحاكمة</c>.</description></item>
    /// <item><term>isAppealable</term><description>Whether a decision of this type can be appealed (boolean, required). Example: <c>false</c>.</description></item>
    /// <item><term>effectId</term><description>Effect: 1 refer, 2 send to archive, 3 join, 4 none (integer, required). Example: <c>2</c>.</description></item>
    /// <item><term>destinationDepartmentId</term><description>Destination department for a referral; forbidden otherwise (GUID, optional). Example: <c>e7a62f43-9b82-4d01-8c2e-7ab91e506d42</c>.</description></item>
    /// <item><term>documentTypeIds</term><description>Document types this decision type applies to (integer array, required). Example: <c>[1]</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated decision type.</response>
    /// <response code="404">No decision type has this id.</response>
    /// <response code="409">An active referral has no destination, or a destination was sent for another effect.</response>
    [HttpPut("{decisionTypeId:guid}")]
    [Authorize(Policy = Permissions.LookupManage)]
    [EndpointSummary("Updates a decision type. Issued decisions keep the snapshot they were given.")]
    [EndpointDescription("Updates a decision type. Issued decisions keep the snapshot they were given. Requires the LOOKUP_MANAGE permission. Responses: 200 (The updated decision type.), 404 (No decision type has this id.), 409 (An active referral has no destination, or a destination was sent for another effect.)")]
    [ProducesResponseType<DecisionTypeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [Description("Decision type id (GUID, route). Example: a1000004-0000-4000-8000-000000000004.")] Guid decisionTypeId,
        [FromBody] UpdateDecisionTypeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateDecisionTypeCommand(
            decisionTypeId,
            request.NameAr,
            request.IsAppealable,
            request.EffectId,
            request.DestinationDepartmentId,
            request.DocumentTypeIds), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Activates a decision type. A referral without a destination is refused.</summary>
    /// <param name="decisionTypeId">Decision type id (GUID, route). Example: <c>a1000002-0000-4000-8000-000000000002</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The activated decision type.</response>
    /// <response code="404">No decision type has this id.</response>
    /// <response code="409">The type is a referral and has no destination department.</response>
    [HttpPost("{decisionTypeId:guid}/activate")]
    [Authorize(Policy = Permissions.LookupManage)]
    [EndpointSummary("Activates a decision type. A referral without a destination is refused.")]
    [EndpointDescription("Activates a decision type. A referral without a destination is refused. Requires the LOOKUP_MANAGE permission. Responses: 200 (The activated decision type.), 404 (No decision type has this id.), 409 (The type is a referral and has no destination department.)")]
    [ProducesResponseType<DecisionTypeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Activate(
        [Description("Decision type id (GUID, route). Example: a1000002-0000-4000-8000-000000000002.")] Guid decisionTypeId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new ActivateDecisionTypeCommand(decisionTypeId), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Deactivates a decision type. A type that was already used stays in the catalog.</summary>
    /// <param name="decisionTypeId">Decision type id (GUID, route). Example: <c>a1000001-0000-4000-8000-000000000001</c>.</param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The deactivated decision type.</response>
    /// <response code="404">No decision type has this id.</response>
    [HttpPost("{decisionTypeId:guid}/deactivate")]
    [Authorize(Policy = Permissions.LookupManage)]
    [EndpointSummary("Deactivates a decision type. A type that was already used stays in the catalog.")]
    [EndpointDescription("Deactivates a decision type. A type that was already used stays in the catalog. Requires the LOOKUP_MANAGE permission. Responses: 200 (The deactivated decision type.), 404 (No decision type has this id.)")]
    [ProducesResponseType<DecisionTypeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        [Description("Decision type id (GUID, route). Example: a1000001-0000-4000-8000-000000000001.")] Guid decisionTypeId,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new DeactivateDecisionTypeCommand(decisionTypeId), cancellationToken);
        return result.ToActionResult();
    }
}
