namespace InstitutionalDiwan.Application.Abstractions.Context;

/// <summary>
/// Ambient request metadata used by logging and the audit trail. Kept host-agnostic so application
/// handlers can record the same correlation id and client address the HTTP layer received.
/// </summary>
public interface IRequestContext
{
    /// <summary>The <c>X-Correlation-Id</c> of the current request, or null outside a request.</summary>
    string? CorrelationId { get; }

    /// <summary>The best-known client IP address, or null when it cannot be determined.</summary>
    string? IpAddress { get; }
}

/// <summary>Keys used to flow request metadata through <c>HttpContext.Items</c>.</summary>
public static class RequestContextKeys
{
    public const string CorrelationId = "InstitutionalDiwan.CorrelationId";
    public const string IpAddress = "InstitutionalDiwan.IpAddress";
}
