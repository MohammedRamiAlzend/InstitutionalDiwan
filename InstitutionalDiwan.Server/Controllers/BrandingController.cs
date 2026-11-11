using InstitutionalDiwan.Application.Features.Organization.InstitutionSettings;
using InstitutionalDiwan.Server.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace InstitutionalDiwan.Server.Controllers;

/// <summary>
/// The institution's public identity — its name and logo — which the sign-in page, the application shell and
/// the browser tab show. Anonymous on purpose: nothing here is confidential and it is needed before login.
/// </summary>
[ApiController]
[Route("api/branding")]
[Produces("application/json")]
[AllowAnonymous]
public sealed class BrandingController : ControllerBase
{
    private readonly ISender _sender;

    public BrandingController(ISender sender) => _sender = sender;

    /// <summary>Returns the institution name and the version of its logo.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The branding. <c>logoUpdatedAt</c> is null when no logo was uploaded.</response>
    /// <response code="404">Institution settings have not been seeded yet.</response>
    [HttpGet]
    [EndpointSummary("Returns the institution name and logo version.")]
    [EndpointDescription("Returns the institution's Arabic and English names and when its logo last changed (null without a logo). Anonymous. Responses: 200 (The branding.), 404 (Institution settings have not been seeded yet.)")]
    [ProducesResponseType<BrandingResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetBrandingQuery(), cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>Streams the uploaded institution logo.</summary>
    /// <param name="cancellationToken">Request cancellation token (optional).</param>
    /// <response code="200">The image bytes with their media type.</response>
    /// <response code="304">The cached copy of the caller is still current.</response>
    /// <response code="404">No logo has been uploaded.</response>
    [HttpGet("logo")]
    [Produces("image/png", "image/jpeg", "image/webp", "image/svg+xml")]
    [EndpointSummary("Streams the uploaded institution logo.")]
    [EndpointDescription("Streams the logo image uploaded in the institution settings. Anonymous. Revalidated with an ETag so a changed logo shows at once. Responses: 200 (The image bytes with their media type.), 304 (The cached copy of the caller is still current.), 404 (No logo has been uploaded.)")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLogo(CancellationToken cancellationToken)
    {
        var result = await _sender.Send(new GetInstitutionLogoQuery(), cancellationToken);
        if (result.IsFailure)
            return result.Error.ToActionResult();

        var logo = result.Value;
        Response.Headers[HeaderNames.CacheControl] = "public, no-cache";
        // An uploaded SVG must stay an image: nothing in it may run if someone opens the address directly.
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        Response.Headers[HeaderNames.ContentSecurityPolicy] = "default-src 'none'; style-src 'unsafe-inline'; sandbox";

        var tag = new EntityTagHeaderValue("\"" + logo.UpdatedAt.Ticks.ToString("x") + "\"");
        var modified = new DateTimeOffset(DateTime.SpecifyKind(logo.UpdatedAt, DateTimeKind.Utc));
        return File(logo.Data, logo.ContentType, modified, tag);
    }
}
