using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

namespace InstitutionalDiwan.Server.Extensions;

public static class RateLimitPolicies
{
    public const string Login = "login";
    public const string Register = "register";
}

/// <summary>
/// Per-address fixed windows for the anonymous authentication endpoints, backstopping the
/// per-username login throttle: 30 login attempts per minute and 5 registrations per hour from
/// one address. Requests are never queued — the excess gets an immediate 429.
/// </summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddInstitutionalRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "RateLimit.Exceeded",
                    Detail = "Too many requests; please wait before trying again."
                }, cancellationToken);
            };

            options.AddPolicy(RateLimitPolicies.Login, PartitionAddress(30, TimeSpan.FromMinutes(1)));
            options.AddPolicy(RateLimitPolicies.Register, PartitionAddress(5, TimeSpan.FromHours(1)));
        });

        return services;
    }

    private static Func<HttpContext, RateLimitPartition<string>> PartitionAddress(
        int permitLimit, TimeSpan window) =>
        context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = window,
                QueueLimit = 0,
                AutoReplenishment = true
            });
}
