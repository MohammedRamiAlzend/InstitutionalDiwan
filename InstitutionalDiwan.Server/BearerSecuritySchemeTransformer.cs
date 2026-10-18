using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace InstitutionalDiwan.Server;

/// <summary>
/// Defines the <see cref="BearerSecuritySchemeTransformer" />
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="BearerSecuritySchemeTransformer"/> class.
/// </remarks>
/// <param name="authenticationSchemeProvider">The authenticationSchemeProvider<see cref="IAuthenticationSchemeProvider"/></param>
internal sealed class BearerSecuritySchemeTransformer(IAuthenticationSchemeProvider authenticationSchemeProvider) : IOpenApiDocumentTransformer
{

    /// <summary>
    /// The TransformAsync
    /// </summary>
    /// <param name="document">The document<see cref="OpenApiDocument"/></param>
    /// <param name="context">The context<see cref="OpenApiDocumentTransformerContext"/></param>
    /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/></param>
    /// <returns>The <see cref="Task"/></returns>
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();
        if (!authenticationSchemes.Any(a => a.Name == "Bearer"))
            return;

        var bearerScheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header using the Bearer scheme."
        };

        document.Components ??= new OpenApiComponents();

        document.AddComponent("Bearer", bearerScheme);
        var securityRequirement = new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer")] = []
        };

        foreach (var path in document.Paths.Values)
        {
            foreach (var operation in path.Operations.Values)
            {
                operation.Security ??= new List<OpenApiSecurityRequirement>();
                operation.Security.Add(securityRequirement);
            }
        }
    }
}