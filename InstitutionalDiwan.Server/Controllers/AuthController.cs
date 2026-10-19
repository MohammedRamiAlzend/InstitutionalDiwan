using InstitutionalDiwan.Application.Features.Identity.ChangePassword;
using InstitutionalDiwan.Application.Features.Identity.Login;
using InstitutionalDiwan.Application.Features.Identity.Register;
using InstitutionalDiwan.Infrastructure.Authentication;
using InstitutionalDiwan.Server.Contracts.Identity;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
[Authorize]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    /// <summary>Authenticates a user and returns a signed JWT access token.</summary>
    /// <response code="200">Authentication succeeded; the access token is returned.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="401">The username or password is incorrect.</response>
    /// <response code="403">The account is inactive.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCommand(request.Username, request.Password),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Creates a new user account and assigns the default USER role.</summary>
    /// <response code="200">The account was created.</response>
    /// <response code="400">The payload failed validation (username too short, password under 8 characters, ...).</response>
    /// <response code="404">The default USER role has not been seeded yet.</response>
    /// <response code="409">The username is already taken.</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterCommand(request.Username, request.Password),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Returns the authenticated caller's identity, roles, and permissions from the token.</summary>
    /// <response code="200">The caller's identity.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return Ok(new MeResponse(
            Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty,
            User.Identity?.Name ?? string.Empty,
            User.FindAll(ClaimTypes.Role).Select(claim => claim.Value).Distinct().ToList(),
            User.FindAll(JwtProvider.PermissionClaimType).Select(claim => claim.Value).Distinct().ToList()));
    }

    /// <summary>Changes the authenticated user's password and returns a fresh access token.</summary>
    /// <response code="200">Password changed; a new access token is returned.</response>
    /// <response code="400">Validation failed, the current password is incorrect, or the new password repeats the old one.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="404">The account no longer exists.</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var result = await _sender.Send(
            new ChangePasswordCommand(userId, request.OldPassword, request.NewPassword),
            cancellationToken);

        return result.ToActionResult();
    }
}
