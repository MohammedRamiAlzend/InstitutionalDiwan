using InstitutionalDiwan.Server.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;

namespace InstitutionalDiwan.Tests.Server;

/// <summary>
/// Verifies that every controller endpoint is fully documented in the generated OpenAPI document:
/// an <c>EndpointSummary</c>, an <c>EndpointDescription</c>, and a description with an example for
/// each route/query parameter.
/// </summary>
public sealed class OpenApiDocumentationTests
{
    private static async Task<Microsoft.OpenApi.OpenApiDocument> GenerateDocumentAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services
            .AddControllers()
            .AddApplicationPart(typeof(AuthController).Assembly);
        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();

        await using var app = builder.Build();
        app.MapControllers();

        var provider = app.Services.GetKeyedService<IOpenApiDocumentProvider>("v1")
            ?? app.Services.GetService<IOpenApiDocumentProvider>();
        if (provider is null)
        {
            var openApiServices = string.Join(", ", builder.Services
                .Where(d => d.ServiceType.FullName?.Contains("OpenApi") == true)
                .Select(d => $"{d.ServiceType.Name} (keyed: {d.IsKeyedService})"));
            throw new InvalidOperationException(
                "IOpenApiDocumentProvider is not registered. OpenApi services found: " + openApiServices);
        }

        return await provider.GetOpenApiDocumentAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Every_endpoint_has_a_summary_and_a_description()
    {
        var document = await GenerateDocumentAsync();

        var operations = document.Paths
            .SelectMany(path => (path.Value.Operations ?? [])
                .Select(operation => (Path: path.Key, Method: operation.Key, Operation: operation.Value)))
            .ToList();

        Assert.NotEmpty(operations);

        var undocumented = operations
            .Where(x =>
                string.IsNullOrWhiteSpace(x.Operation.Summary) ||
                string.IsNullOrWhiteSpace(x.Operation.Description))
            .Select(x => $"{x.Method} {x.Path}")
            .ToList();

        Assert.True(
            undocumented.Count == 0,
            "Endpoints missing [EndpointSummary]/[EndpointDescription]: " + string.Join(", ", undocumented));
    }

    [Fact]
    public async Task Every_route_and_query_parameter_documents_its_content_with_an_example()
    {
        var document = await GenerateDocumentAsync();

        var problems = new List<string>();
        foreach (var (path, item) in document.Paths)
        {
            foreach (var (method, operation) in item.Operations ?? [])
            {
                foreach (var parameter in operation.Parameters ?? [])
                {
                    if (string.IsNullOrWhiteSpace(parameter.Description))
                        problems.Add($"{method} {path} parameter '{parameter.Name}' has no description");
                    else if (!parameter.Description.Contains("Example", StringComparison.OrdinalIgnoreCase))
                        problems.Add($"{method} {path} parameter '{parameter.Name}' has no example");
                }
            }
        }

        Assert.True(problems.Count == 0, string.Join("; ", problems));
    }

    [Fact]
    public async Task Known_endpoint_publishes_its_documented_behaviour()
    {
        var document = await GenerateDocumentAsync();

        var receive = document.Paths["/api/movements/{movementId}/receive"].Operations[System.Net.Http.HttpMethod.Post];

        Assert.Equal("Receives the movement's current step at the destination department.", receive.Summary);
        Assert.Contains("DOCUMENT_EDIT", receive.Description);
        Assert.Contains("Responses: 200", receive.Description);

        var movementId = receive.Parameters!.Single(p => p.Name == "movementId");
        Assert.Contains("1a9d4e7b-2c58-4f3a-8e61-9b0d7c2f5a84", movementId.Description);
    }
}
