# Application / Behaviors

> **Status:** Partially implemented — `ValidationPipelineBehavior` is live and registered by
> `AddApplication()`; logging, transaction, and caching behaviors remain target design.

## Purpose

This folder holds MediatR `IPipelineBehavior<TRequest, TResponse>` classes that wrap every command
and query with cross-cutting concerns. Handlers stay focused on domain orchestration; validation,
logging, transactions, and caching are handled uniformly by the pipeline.

Recommended registration order (outermost → innermost):
`Logging → Validation → QueryCaching → Transaction → Handler`.

## Planned behaviors

| Behavior | Responsibility |
| --- | --- |
| `ValidationPipelineBehavior` | Runs all registered `FluentValidation.IValidator<TRequest>` validators before the handler. On failure it **short-circuits** and returns `Result.Failure(ErrorType.Validation)` — it does not throw, keeping validation a first-class domain outcome. |
| `LoggingPipelineBehavior` | Structured start/completion/failure logs including `RequestName`, `UserId`, `DepartmentId`, `CorrelationId`, and elapsed milliseconds. When `result.IsFailure` it logs `Error.Code` / `Error.Description` at Warning/Error level. |
| `TransactionPipelineBehavior` | Wraps state-mutating `ICommand` executions in an atomic `IUnitOfWork` transaction so aggregate changes, domain events, and outbox rows commit together. Queries bypass it. |
| `QueryCachingPipelineBehavior` | Intercepts queries implementing `ICacheableQuery` and serves the cached `Result<TResponse>` when present, writing back on a miss. |

## Design notes

- Validation errors are returned as `Result`, so the API maps them to HTTP 400 via the same
  `ResultExtensions` path used for every other failure — no special-casing.
- The transaction behavior must forward the `CancellationToken` and dispose/rollback on exception.
- Behaviors are generic and must remain infrastructure-agnostic (no EF Core / ASP.NET types).

## Contents

| File | Purpose |
| --- | --- |
| `ValidationPipelineBehavior.cs` | Runs every registered `IValidator<TRequest>` before the handler; on failure it short-circuits and returns `Result.Failure(Error.Validation(code, messages))` using the first FluentValidation error code, so validation stays a domain outcome instead of an exception. Registered as an open behavior by `Application/DependencyInjection.cs`. |

## Implementation status

| Behavior | State |
| --- | --- |
| `ValidationPipelineBehavior` | ✅ Implemented (returns a failed `Result`, never throws) |
| `LoggingPipelineBehavior` | ❌ Not implemented |
| `TransactionPipelineBehavior` | ❌ Not implemented |
| `QueryCachingPipelineBehavior` | ❌ Not implemented |

The remaining behaviors depend on abstractions that are not wired up yet (`ICacheableQuery`,
`IUserContext`); see [`../Abstractions/README.md`](../Abstractions/README.md).

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-19 | opencode | Recorded `ValidationPipelineBehavior` as ✅ implemented with a Contents row (the README previously reported it as not implemented). |
