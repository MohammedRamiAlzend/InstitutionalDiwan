# Presentation / Middleware

> **Status:** Partially implemented — `GlobalExceptionHandlingMiddleware` is implemented and
> registered outermost in `Program.cs`; `RequestContextLoggingMiddleware` remains target design.

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
Reads or generates the `X-Correlation-Id` header and pushes `CorrelationId`, `IpAddress`, and `UserId`
into the structured logging scope (`LogContext.PushProperty`) and the audit context, so every log line
and `AUDIT_LOGS.CorrelationId` value can be tied back to a single request.

## Pipeline position

Register exception handling **outermost**, then request-context logging, then authorization, then
controllers:

```
GlobalExceptionHandling → RequestContextLogging → Authentication → Authorization → Endpoints
```

## Implementation status

| Middleware | State |
| --- | --- |
| `GlobalExceptionHandlingMiddleware` | ✅ Implemented (500 fallback + 409 for unique-key violations) |
| `RequestContextLoggingMiddleware` | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-18 | opencode | `GlobalExceptionHandlingMiddleware` now maps SQL Server unique-key violations (`DbUpdateException`, errors 2601/2627) to `409 Conflict` — concurrent duplicate official numbers return 409 instead of 500. |
