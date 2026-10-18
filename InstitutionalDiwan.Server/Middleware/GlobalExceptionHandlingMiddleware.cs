using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace InstitutionalDiwan.Server.Middleware;

/// <summary>
/// Catches unhandled exceptions, logs them, and returns a clean RFC 7807 <see cref="ProblemDetails"/>
/// response without leaking stack traces. Database unique-key violations (the last line of defence
/// behind application-level duplicate checks such as the official number + year check) are mapped
/// to <c>409 Conflict</c> instead of <c>500</c>.
/// </summary>
public sealed class GlobalExceptionHandlingMiddleware
{
    private const int SqlServerUniqueIndexError = 2601;
    private const int SqlServerUniqueConstraintError = 2627;

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

    public GlobalExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            _logger.LogWarning(exception, "Unique-key violation while processing {Method} {Path}.",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status409Conflict;

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "A conflicting record already exists.",
                Detail = "The submitted record conflicts with data that already exists.",
                Type = "https://httpstatuses.io/409"
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}.",
                context.Request.Method, context.Request.Path);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;

            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https://httpstatuses.io/500"
            });
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException sqlException &&
        (sqlException.Number is SqlServerUniqueIndexError or SqlServerUniqueConstraintError);
}
