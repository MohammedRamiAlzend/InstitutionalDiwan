namespace InstitutionalDiwan.Server.Middleware;

/// <summary>
/// Adds the baseline browser-security headers to every response: nosniff, frame denial,
/// no-referrer, HSTS outside Development, and no-store on the authentication endpoints so
/// tokens/credentials never hit a shared cache.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IHostEnvironment _environment;

    public SecurityHeadersMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        _next = next;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";

        // HSTS over plain HTTP would brick a development loop; browsers honor it on HTTPS only.
        if (!_environment.IsDevelopment())
            headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";

        if (context.Request.Path.StartsWithSegments("/api/auth"))
            headers["Cache-Control"] = "no-store";

        await _next(context);
    }
}
