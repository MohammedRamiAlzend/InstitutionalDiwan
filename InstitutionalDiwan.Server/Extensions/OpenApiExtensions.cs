using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace InstitutionalDiwan.Server.Extensions;

public static class OpenApiExtensions
{
    /// <summary>
    /// Registers the OpenAPI document and decorates it with a Bearer (JWT) security scheme so the
    /// Scalar UI can authorize requests.
    /// </summary>
    public static IServiceCollection AddInstitutionalOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>());

        return services;
    }
}

internal sealed class BearerSecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Paste the JWT access token returned by POST /api/auth/login."
        };

        return Task.CompletedTask;
    }
}
