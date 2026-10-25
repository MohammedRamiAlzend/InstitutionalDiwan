using InstitutionalDiwan.Application.Abstractions.Context;
using Microsoft.AspNetCore.Http;

namespace InstitutionalDiwan.Infrastructure.Context;

/// <summary>Projects the correlation id and client address set by the request middleware.</summary>
internal sealed class HttpRequestContext : IRequestContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpRequestContext(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    public string? CorrelationId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
                return null;

            return context.Items.TryGetValue(RequestContextKeys.CorrelationId, out var value)
                ? value as string
                : null;
        }
    }

    public string? IpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context is null)
                return null;

            return context.Items.TryGetValue(RequestContextKeys.IpAddress, out var value)
                ? value as string
                : context.Connection.RemoteIpAddress?.ToString();
        }
    }
}
