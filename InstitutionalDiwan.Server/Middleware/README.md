# Presentation / Middleware

> **Status:** Implemented — `RequestContextLoggingMiddleware` is registered first and
> `GlobalExceptionHandlingMiddleware` immediately after it, so correlation ids reach both the
> exception log and every inner handler.

## Purpose

ASP.NET Core HTTP pipeline middleware that applies request-wide concerns before controllers run.

## Components

### `GlobalExceptionHandlingMiddleware.cs`
Catches any unhandled exception (such as a database connectivity failure), logs it, and returns a
clean RFC 7807 `ProblemDetails` JSON response **without leaking stack traces**. Two paths:

- **`DbUpdateException` caused by a SQL Server unique-key violation (error 2601/2627)** → `409
  Conflict` with a generic "conflicting record already exists" problem — the last line of defence
  behind application-level duplicate checks such as the official number + year check in
  `CreateDocumentCommandHandler`.
- **Anything else** → `500 Internal Server Error`, logged at `Error` level (the conflict path is
  logged at `Warning`).

### `RequestContextLoggingMiddleware.cs`
Reads or generates the `X-Correlation-Id` header, stores it (and the client IP) in
`HttpContext.Items` for `HttpRequestContext` and the audit interceptor, echoes it on the response,
and pushes `CorrelationId`, `IpAddress`, and `UserId` into the structured logging scope. Every log
line and `AUDIT_LOGS.CorrelationId` can therefore be tied back to one request.

## Pipeline position

Register request-context logging **outermost**, then exception handling, then authorization, then
controllers:

```
RequestContextLogging → GlobalExceptionHandling → Authentication → Authorization → Endpoints
```

Request-context logging must come first so the correlation id is already in the logging scope when
an exception is recorded and is available to controllers that fail early.

## Implementation status

| Middleware | State |
| --- | --- |
| `GlobalExceptionHandlingMiddleware` | ✅ Implemented (500 fallback + 409 for unique-key violations) |
| `RequestContextLoggingMiddleware` | ✅ Implemented (correlation id, IP, user id, response header) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Implemented `RequestContextLoggingMiddleware` for Module 09 and corrected the pipeline order (request context outermost, then exception handling). |
| 2026-10-18 | opencode | `GlobalExceptionHandlingMiddleware` now maps SQL Server unique-key violations (`DbUpdateException`, errors 2601/2627) to `409 Conflict` — concurrent duplicate official numbers return 409 instead of 500. |
