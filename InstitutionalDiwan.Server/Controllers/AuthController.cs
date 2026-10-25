using InstitutionalDiwan.Application.Features.Identity.ChangePassword;
using InstitutionalDiwan.Application.Features.Identity.Login;
using InstitutionalDiwan.Application.Features.Identity.Register;
using InstitutionalDiwan.Server.Contracts.Identity;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

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
    /// <remarks>
    /// Use this endpoint to obtain an access token for subsequent authenticated requests.
    /// The token should be sent in the Authorization header as: <c>Bearer {token}</c>.
    /// </remarks>
    /// <param name="request">
    /// Login credentials payload.
    /// <list type="bullet">
    /// <item><term>username</term><description>Unique account username (string). Example: <c>admin</c></description></item>
    /// <item><term>password</term><description>User's password (string). Example: <c>P@ssw0rd123</c></description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">Authentication succeeded; the access token is returned in <c>LoginResponse</c>.</response>
    /// <response code="400">The request payload failed validation.</response>
    /// <response code="401">The username or password is incorrect.</response>
    /// <response code="403">The account is inactive.</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [EndpointSummary("Authenticate user")]
    [EndpointDescription("Authenticates a user with username and password and returns a signed JWT access token. Use the token in the Authorization header as 'Bearer {token}'.")]
    [Produces("application/json", Type = typeof(LoginResponse))]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Login(
        [FromBody, Description("Login credentials containing username and password.")] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new LoginCommand(request.Username, request.Password),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Creates a new user account and assigns the default USER role.</summary>
    /// <remarks>
    /// Registers a new user with the provided credentials. On success, the default USER role is assigned.
    /// </remarks>
    /// <param name="request">
    /// Registration payload.
    /// <list type="bullet">
    /// <item><term>username</term><description>Desired username (string, min length as per validation). Example: <c>johndoe</c></description></item>
    /// <item><term>password</term><description>Password (string, must meet complexity requirements, min 8 characters). Example: <c>Str0ngP@ss!</c></description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The account was created; returns <c>RegisterResponse</c>.</response>
    /// <response code="400">The payload failed validation (username too short, password under 8 characters, etc.).</response>
    /// <response code="404">The default USER role has not been seeded yet.</response>
    /// <response code="409">The username is already taken.</response>
    [HttpPost("register")]
    [EndpointSummary("Creates a new user account and assigns the default USER role.")]
    [EndpointDescription("Creates a new user account and assigns the default USER role. Registers a new user with the provided credentials. On success, the default USER role is assigned. Responses: 200 (The account was created; returns RegisterResponse.), 400 (The payload failed validation (username too short, password under 8 characters, etc.).), 404 (The default USER role has not been seeded yet.), 409 (The username is already taken.)")]
    [AllowAnonymous]
    [Produces("application/json", Type = typeof(RegisterResponse))]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody, Description("Registration details containing username and password.")] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _sender.Send(
            new RegisterCommand(request.Username, request.Password),
            cancellationToken);

        return result.ToActionResult();
    }

    /// <summary>Returns the authenticated caller's identity, roles, and permissions from the token.</summary>
    /// <remarks>
    /// Reads the identity the host projects from the current JWT access token — user id, username,
    /// role codes, and permission codes — through the application layer's
    /// <c>GetUserProfileQuery</c>. Requires a valid Bearer token in the Authorization header.
    /// </remarks>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The caller's profile returned as <c>UserProfileResponse</c>.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    [HttpGet("me")]
    [EndpointSummary("Returns the authenticated caller's identity, roles, and permissions from the token.")]
    [EndpointDescription("Returns the authenticated caller's identity, roles, and permissions from the token. Reads the identity the host projects from the current JWT access token — user id, username, role codes, and permission codes. Requires a valid Bearer token in the Authorization header. Responses: 200 (The caller's profile returned as UserProfileResponse.), 401 (The token is missing, expired, or invalid.)")]
    [Authorize]
    [Produces("application/json", Type = typeof(UserProfileResponse))]
    [ProducesResponseType<UserProfileResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetUserProfileQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Changes the authenticated user's password and returns a fresh access token.</summary>
    /// <remarks>
    /// Updates the caller's password after validating the current password. On success, a new JWT access token is issued.
    /// Requires a valid Bearer token in the Authorization header.
    /// </remarks>
    /// <param name="request">
    /// Password change payload.
    /// <list type="bullet">
    /// <item><term>oldPassword</term><description>Current password (string). Example: <c>OldP@ssw0rd</c></description></item>
    /// <item><term>newPassword</term><description>New password (string, must meet complexity requirements and differ from old password). Example: <c>NewP@ssw0rd123</c></description></item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">Password changed successfully; a fresh access token is returned as <c>LoginResponse</c>.</response>
    /// <response code="400">Validation failed, the current password is incorrect, or the new password repeats the old one.</response>
    /// <response code="401">The token is missing, expired, or invalid.</response>
    /// <response code="404">The account no longer exists.</response>
    [HttpPost("change-password")]
    [EndpointSummary("Changes the authenticated user's password and returns a fresh access token.")]
    [EndpointDescription("Changes the authenticated user's password and returns a fresh access token. Updates the caller's password after validating the current password. On success, a new JWT access token is issued. Requires a valid Bearer token in the Authorization header. Responses: 200 (Password changed successfully; a fresh access token is returned as LoginResponse.), 400 (Validation failed, the current password is incorrect, or the new password repeats the old one.), 401 (The token is missing, expired, or invalid.), 404 (The account no longer exists.)")]
    [Authorize]
    [Produces("application/json", Type = typeof(LoginResponse))]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        [FromBody, Description("Password change details containing current and new passwords.")] ChangePasswordRequest request,
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
