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
    /// <response code="200">The settings.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="403">The token lacks the DEPARTMENT_MANAGE permission.</response>
    /// <response code="404">Institution settings have not been seeded yet.</response>
    [HttpGet]
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
    /// <response code="200">The updated settings.</response>
    /// <response code="400">The payload failed validation, or referenced an unknown lookup entry.</response>
    /// <response code="404">Institution settings have not been seeded yet.</response>
    /// <response code="409">The Central Diwan is inactive, not a root, or is the wrong type.</response>
    [HttpPut]
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
