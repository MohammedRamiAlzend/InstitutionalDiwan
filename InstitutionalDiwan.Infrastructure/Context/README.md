# Infrastructure / Context

> **Status:** Implemented — `HttpRequestContext` projects the ambient HTTP request metadata.

## Purpose

Expose request-scoped metadata (correlation id and client address) to the Application layer through
`IRequestContext`, without letting handlers depend on ASP.NET Core.

## Component

### `HttpRequestContext.cs` (`: IRequestContext`)
- Reads `InstitutionalDiwan.CorrelationId` / `InstitutionalDiwan.IpAddress` from
  `HttpContext.Items`, where `RequestContextLoggingMiddleware` placed them.
- Falls back to `HttpContext.Connection.RemoteIpAddress` when the IP item is missing.
- Returns `null` for both values outside a request (background jobs, startup seeding).

It is registered as a scoped service by `AddInfrastructure` and is injected into
`AuditLogAppender` and `LoggingPipelineBehavior`.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Added `HttpRequestContext` for Module 09 request metadata. |
