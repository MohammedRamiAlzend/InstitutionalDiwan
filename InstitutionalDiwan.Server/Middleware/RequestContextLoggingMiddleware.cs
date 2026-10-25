using InstitutionalDiwan.Application.Abstractions.Context;
using System.Security.Claims;

namespace InstitutionalDiwan.Server.Middleware;

/// <summary>
/// Establishes one correlation id for the request (from <c>X-Correlation-Id</c> or the trace id),
/// exposes it and the client address to the audit trail, echoes it on the response, and pushes both
/// values into the logging scope used by the MediatR logging behavior.
/// </summary>
public sealed class RequestContextLoggingMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-Id";

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestContextLoggingMiddleware> _logger;

    public RequestContextLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestContextLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = context.TraceIdentifier;

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        context.Items[RequestContextKeys.CorrelationId] = correlationId;
        if (ipAddress is not null)
            context.Items[RequestContextKeys.IpAddress] = ipAddress;

        context.Response.Headers[CorrelationIdHeader] = correlationId;

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["UserId"] = userId,
            ["IpAddress"] = ipAddress
        }))
        {
            await _next(context);
        }
    }
}
