namespace InstitutionalDiwan.Server.Middleware;

/// <summary>
/// Keeps a high-frequency, read-only route (a polled badge count) out of the action audit trail so it
/// cannot flood the hash chain. Never put it on a route that changes data.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SkipActionAuditAttribute : Attribute
{
}
