using InstitutionalDiwan.Application.Features.Identity.Login;
using InstitutionalDiwan.Application.Features.Identity.Register;
using InstitutionalDiwan.Server.Contracts.Identity;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
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
}
