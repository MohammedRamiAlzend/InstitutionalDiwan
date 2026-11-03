using InstitutionalDiwan.Server.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace InstitutionalDiwan.Tests.Server;

public sealed class GlobalExceptionHandlingMiddlewareTests
{
    private static async Task<(int Status, JsonElement Body)> InvokeAsync(Exception exception)
    {
        var context = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddOptions().BuildServiceProvider()
        };
        context.Response.Body = new MemoryStream();

        var middleware = new GlobalExceptionHandlingMiddleware(
            _ => throw exception,
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(context.Response.Body);
        return (context.Response.StatusCode, document.RootElement.Clone());
    }

    [Fact]
    public async Task ConcurrencyConflict_AnswersConflictWithStableCode()
    {
        var (status, body) = await InvokeAsync(new DbUpdateConcurrencyException("stale row"));

        Assert.Equal(StatusCodes.Status409Conflict, status);
        Assert.Equal("Common.ConcurrencyConflict", body.GetProperty("title").GetString());
    }

    [Fact]
    public async Task OtherDatabaseFailure_StaysAnInternalError()
    {
        var (status, _) = await InvokeAsync(new DbUpdateException("constraint"));

        Assert.Equal(StatusCodes.Status500InternalServerError, status);
    }
}
