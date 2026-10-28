# Presentation / Middleware

> **Status:** Implemented — `RequestContextLoggingMiddleware` is registered first,
> `GlobalExceptionHandlingMiddleware` immediately after it, and `AuditActionMiddleware` between
> authentication and authorization, so correlation ids reach every handler and every authenticated
> `/api` action (including reads and failures) reaches `AUDIT_LOGS`.

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

### `AuditActionMiddleware.cs`
Wraps every **authenticated** `/api` controller action and appends one action-level audit row through
`IAuditActionRecorder`: module derived from the controller (`Documents` → `CORRESPONDENCE`,
`DocumentFiles` → `ATTACHMENTS`, `Movements` → `MOVEMENTS`, …), action derived from the route
(`Create`/`Update`/`View`/`Delete`/`Execute` — `Reset*` actions such as
`Users.ResetPassword` count as `Update`), entity type `Operation`, entity id
`{Controller}.{Action}`, actor user/department, and new-values JSON with HTTP method, path, status
code, duration, and the error type when the action threw. This is what makes reads, rejected calls
(403/404/409), and exceptions visible in the audit table. It skips OPTIONS, non-`/api` paths,
non-controller endpoints, and unauthenticated requests (login/renew stay anonymous — a successful
login is audited through `UserLoggedInEvent` instead). Recording happens in `finally` with
`CancellationToken.None` and the exception is rethrown, so `GlobalExceptionHandlingMiddleware` still
produces the response.

## Pipeline position

Register request-context logging **outermost**, then exception handling, then authentication, then
the audit action middleware, then authorization, then controllers:

```
RequestContextLogging → GlobalExceptionHandling → Authentication → AuditActionMiddleware → Authorization → Endpoints
```

Request-context logging must come first so the correlation id is already in the logging scope when
an exception is recorded and is available to controllers that fail early. The audit middleware needs
an authenticated caller (it reads the user id/department) and must sit **before** authorization so
rejected-but-authenticated calls are still recorded; it sits inside exception handling so it can see
the exception, record the failure, and rethrow.

## Implementation status

| Middleware | State |
| --- | --- |
| `GlobalExceptionHandlingMiddleware` | ✅ Implemented (500 fallback + 409 for unique-key violations) |
| `RequestContextLoggingMiddleware` | ✅ Implemented (correlation id, IP, user id, response header) |
| `AuditActionMiddleware` | ✅ Implemented (one `Operation` row per authenticated `/api` action, including reads and failures) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-26 | Qoder | Added `AuditActionMiddleware` between authentication and authorization so every authenticated `/api` action — including reads, 4xx/5xx results, and exceptions — is recorded as an `Operation` row; the recorder uses an isolated scope and never alters the response. |
| 2026-10-25 | opencode | Implemented `RequestContextLoggingMiddleware` for Module 09 and corrected the pipeline order (request context outermost, then exception handling). |
| 2026-10-18 | opencode | `GlobalExceptionHandlingMiddleware` now maps SQL Server unique-key violations (`DbUpdateException`, errors 2601/2627) to `409 Conflict` — concurrent duplicate official numbers return 409 instead of 500. |
