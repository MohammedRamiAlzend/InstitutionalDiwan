using InstitutionalDiwan.Domain.Entities.Diagnostics;

namespace InstitutionalDiwan.Server.Contracts.Diagnostics;

/// <summary>Payload submitted to <c>PUT /api/system-errors/{id}</c>.</summary>
public sealed record UpdateSystemErrorRequest(SystemErrorStatus Status, string? Notes);

/// <summary>Payload submitted to <c>POST /api/system-errors/purge</c>.</summary>
public sealed record PurgeSystemErrorsRequest(int OlderThanDays);

/// <summary>A failure seen in the browser; <c>Path</c> never carries a query string.</summary>
public sealed record ClientErrorReportRequest(
    SystemErrorKind Kind,
    string Message,
    string? Details = null,
    string? Path = null,
    string? HttpMethod = null,
    int? StatusCode = null);
