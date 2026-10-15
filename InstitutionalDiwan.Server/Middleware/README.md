# Presentation / Middleware

> **Status:** Target design — **no code yet**.

## Purpose

ASP.NET Core HTTP pipeline middleware that applies request-wide concerns before controllers run.

## Planned components

### `GlobalExceptionHandlingMiddleware.cs`
Catches any unhandled exception (such as the application `ConcurrencyException` or a database
connectivity failure), logs it with the active `CorrelationId`, and returns a clean RFC 7807
`ProblemDetails` JSON response **without leaking stack traces**. Pairs with the application exceptions in
[`../../InstitutionalDiwan.Application/Exceptions/`](../../InstitutionalDiwan.Application/Exceptions/README.md):
`ConcurrencyException → 409`, unhandled `DomainException → 400/500` depending on severity.

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
| `GlobalExceptionHandlingMiddleware` | ❌ Not implemented |
| `RequestContextLoggingMiddleware` | ❌ Not implemented |

Neither is registered in `Program.cs`, so an unhandled exception currently returns the framework default
(and, in Development, would expose the developer exception page).
