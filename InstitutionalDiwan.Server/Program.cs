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

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

app.UseDefaultFiles();
app.MapStaticAssets();

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
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("/index.html");

app.Run();
