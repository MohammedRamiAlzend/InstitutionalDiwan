using InstitutionalDiwan.Application;
using InstitutionalDiwan.Infrastructure;
using InstitutionalDiwan.Server.Extensions;
using InstitutionalDiwan.Server.Middleware;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddInstitutionalOpenApi();
builder.Services.AddInstitutionalAuthentication(builder.Configuration);
builder.Services.AddOpenApi("v1", options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
});
var app = builder.Build();

app.UseMiddleware<RequestContextLoggingMiddleware>();
app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHttpsRedirection();
}

// Ordered, environment-aware seeding — each seeder declares which environments it runs in.
await app.InitializeDatabaseAsync();

app.UseAuthentication();

// Wraps every authenticated /api action, including reads and authorization failures, so the
// action itself is recorded even when it changes no entity.
app.UseMiddleware<AuditActionMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();
