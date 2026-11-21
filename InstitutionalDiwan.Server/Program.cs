using InstitutionalDiwan.Application;
using InstitutionalDiwan.Infrastructure;
using InstitutionalDiwan.Server.Extensions;
using InstitutionalDiwan.Server.Infrastructure;
using InstitutionalDiwan.Server.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Supplies Jwt:SecretKey before authentication is registered. An explicit value (user-secrets in
// Development, Jwt__SecretKey in production) wins; otherwise a key is generated once and persisted
// under keys/ so restarts and redeployments keep the same signer.
var jwtSigningKey = builder.AddInstitutionalJwtSigningKey();

builder.WebHost.ConfigureKestrel(serverOptions => serverOptions.AddServerHeader = false);
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddScoped<InstitutionalDiwan.Application.Abstractions.Notifications.IRealtimeNotifier, InstitutionalDiwan.Server.Hubs.SignalRRealtimeNotifier>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddInstitutionalOpenApi();
builder.Services.AddInstitutionalRateLimiting();
builder.Services.AddInstitutionalAuthentication(builder.Configuration);
builder.Services.AddSeaweedFs(builder.Configuration);

builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
var app = builder.Build();

if (jwtSigningKey.Generated)
{
    app.Logger.LogWarning(
        "Jwt:SecretKey was not configured, so a signing key was generated at {KeyPath}. Keep that file " +
        "with the deployment — replacing it invalidates every issued token. Set Jwt__SecretKey (with an " +
        "optional Jwt__SigningKeyPath) to supply your own instead.",
        jwtSigningKey.KeyPath);
}

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseMiddleware<RequestContextLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    // Redirect only when this instance actually terminates TLS. The plain-HTTP publish profile
    // binds http://0.0.0.0:4567, where a redirect has no target and Kestrel logs
    // "Failed to determine the https port" on every request.
    var configuredUrls = app.Configuration["Urls"]
        ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
        ?? string.Empty;

    if (configuredUrls.Contains("https://", StringComparison.OrdinalIgnoreCase))
    {
        app.UseHttpsRedirection();
    }
}

// Ordered, environment-aware seeding — each seeder declares which environments it runs in.
await app.InitializeDatabaseAsync();

app.UseAuthentication();

// Wraps every authenticated /api action, including reads and authorization failures, so the
// action itself is recorded even when it changes no entity.
app.UseMiddleware<AuditActionMiddleware>();
app.UseMiddleware<ForcedPasswordChangeMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHub<InstitutionalDiwan.Server.Hubs.NotificationsHub>(InstitutionalDiwan.Server.Hubs.NotificationsHub.Route);
app.MapFallbackToFile("/index.html");

app.Run();
