using InstitutionalDiwan.Application.Abstractions.Authentication;
using InstitutionalDiwan.Application.Abstractions.Time;
using InstitutionalDiwan.Application.Features.Diagnostics.SystemErrors;
using Microsoft.AspNetCore.Mvc;

namespace InstitutionalDiwan.Server.Middleware;

/// <summary>
/// Feeds the developer's error log from the request pipeline. Capture is best-effort: whatever goes
/// wrong here is swallowed, because it must never change the response of the request it observes.
/// </summary>
internal static class SystemErrorCapture
{
    public const string ProblemItemKey = "SystemErrorCapture.Problem";

    private const string ApiPrefix = "/api";
    private const string OwnPrefix = "/api/system-errors";

    public static void RecordException(HttpContext context, Exception exception, int statusCode)
    {
        // A client that hung up is not a defect.
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            return;

        Record(context, details => SystemErrorOccurrenceFactory.ForException(exception, statusCode, details));
    }

    /// <summary>Records a finished API request that failed without an exception. 401 is just a signed-out caller.</summary>
    public static void RecordRefusal(HttpContext context)
    {
        var statusCode = context.Response.StatusCode;
        if (statusCode < StatusCodes.Status400BadRequest || statusCode == StatusCodes.Status401Unauthorized)
            return;

        var path = context.Request.Path;
        // The log's own endpoints are left out so a failing report cannot feed itself.
        if (!path.StartsWithSegments(ApiPrefix) || path.StartsWithSegments(OwnPrefix))
            return;

        var problem = context.Items.TryGetValue(ProblemItemKey, out var item) ? item as ProblemDetails : null;
        Record(context, details => SystemErrorOccurrenceFactory.ForRejection(
            problem?.Title ?? $"HTTP {statusCode}", problem?.Detail, statusCode, details));
    }

    private static void Record(
        HttpContext context,
        Func<SystemErrorContext, Domain.ValueObjects.SystemErrorOccurrence> build)
    {
        try
        {
            var services = context.RequestServices;
            var user = services.GetRequiredService<IUserContext>();
            var signedIn = context.User.Identity?.IsAuthenticated == true && user.IsAuthenticated;

            services.GetRequiredService<ISystemErrorQueue>().Enqueue(build(new SystemErrorContext(
                context.Request.Path.Value,
                context.Request.Method,
                signedIn ? user.UserId : null,
                signedIn ? user.Username : null,
                context.Request.Headers.UserAgent.ToString(),
                context.TraceIdentifier,
                services.GetRequiredService<IDateTimeProvider>().UtcNow)));
        }
        catch (Exception)
        {
            // See the class summary.
        }
    }
}
