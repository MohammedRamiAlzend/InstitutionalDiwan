using InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Server.Contracts.Organization;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// The singleton institution configuration (Central Diwan, numbering defaults, responsible
/// employee). Reads and writes both require <c>DEPARTMENT_MANAGE</c> because the Central Diwan
/// designation restructures the organization.
/// </summary>
[ApiController]
[Route("api/institution-settings")]
[Produces("application/json")]
[Authorize(Policy = Permissions.DepartmentManage)]
public sealed class InstitutionSettingsController : ControllerBase
{
    private readonly ISender _sender;

    public InstitutionSettingsController(ISender sender) => _sender = sender;

    /// <summary>Returns the current institution settings.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The settings.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DEPARTMENT_MANAGE permission.</response>
    /// <response code="404">Institution settings have not been seeded yet.</response>
    [HttpGet]
    [EndpointSummary("Returns the current institution settings.")]
    [EndpointDescription("Returns the current institution settings. Requires the DEPARTMENT_MANAGE permission. Responses: 200 (The settings.), 401 (The token is missing, expired, or invalid.), 403 (The token lacks the DEPARTMENT_MANAGE permission.), 404 (Institution settings have not been seeded yet.)")]
    [ProducesResponseType<InstitutionSettingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetInstitutionSettingsQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Replaces the institution settings.</summary>
    /// <remarks>
    /// The Central Diwan department must exist, be active, be of type CENTRAL_DIWAN, and be a root
    /// department. Changing the Central Diwan requires a transfer reason.
    /// </remarks>
    /// <param name="request">
    /// The request payload with the following fields:
    /// <list type="bullet">
    /// <item><term>institutionNameAr</term><description>Institution name in Arabic (string, required). Example: <c>الديوان المركزي</c>.</description></item>
    /// <item><term>numberingModeId</term><description>Numbering mode id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>numberingResetPolicyId</term><description>Numbering reset policy id (integer, required). Example: <c>1</c>.</description></item>
    /// <item><term>centralDiwanDepartmentId</term><description>Central diwan department unique identifier (GUID, optional). Example: <c>1c3a5e80-2f47-4b96-a5d1-6e0c8b3f7a25</c>.</description></item>
    /// <item><term>institutionNameEn</term><description>Institution name in English (string, optional). Example: <c>Central Diwan</c>.</description></item>
    /// <item><term>logoPath</term><description>Logo file path (string, optional). Example: <c>/assets/logo.png</c>.</description></item>
    /// <item><term>defaultTimezone</term><description>Default time zone (string, optional). Example: <c>Asia/Riyadh</c>.</description></item>
    /// <item><term>responsibleEmployeeId</term><description>Responsible employee unique identifier (GUID, optional). Example: <c>3fa85f64-5717-4562-b3fc-2c963f66afa6</c>.</description></item>
    /// <item><term>centralDiwanTransferReason</term><description>Central Diwan transfer reason (string, optional). Example: <c>Reassignment of the Central Diwan designation</c>.</description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The updated settings.</response>
    /// <response code="400">The payload failed validation, or referenced an unknown lookup entry.</response>
    /// <response code="404">Institution settings have not been seeded yet.</response>
    /// <response code="409">The Central Diwan is inactive, not a root, or is the wrong type.</response>
    [HttpPut]
    [EndpointSummary("Replaces the institution settings.")]
    [EndpointDescription("Replaces the institution settings. The Central Diwan department must exist, be active, be of type CENTRAL_DIWAN, and be a root department. Changing the Central Diwan requires a transfer reason. Requires the DEPARTMENT_MANAGE permission. Responses: 200 (The updated settings.), 400 (The payload failed validation, or referenced an unknown lookup entry.), 404 (Institution settings have not been seeded yet.), 409 (The Central Diwan is inactive, not a root, or is the wrong type.)")]
    [ProducesResponseType<InstitutionSettingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromBody] UpdateInstitutionSettingsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new UpdateInstitutionSettingsCommand(
            request.InstitutionNameAr,
            request.NumberingModeId,
            request.NumberingResetPolicyId,
            request.CentralDiwanDepartmentId,
            request.ResponsibleEmployeeId,
            request.InstitutionNameEn,
            request.LogoPath,
            request.DefaultTimezone,
            request.CentralDiwanTransferReason), cancellationToken);

        return result.ToActionResult();
    }
}
