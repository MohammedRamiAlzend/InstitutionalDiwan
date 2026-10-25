# Application / Behaviors

> **Status:** Partially implemented — `ValidationPipelineBehavior`, `LoggingPipelineBehavior`, and
> `TransactionPipelineBehavior` are live and registered by `AddApplication()`; the caching behavior
> remains target design.

## Purpose

This folder holds MediatR `IPipelineBehavior<TRequest, TResponse>` classes that wrap every command
and query with cross-cutting concerns. Handlers stay focused on domain orchestration; validation,
logging, transactions, and caching are handled uniformly by the pipeline.

Registered order in `Application/DependencyInjection.cs` (outermost → innermost):
`Validation → Logging → Transaction → Handler`.

## Behaviors

| Behavior | Responsibility |
| --- | --- |
| `ValidationPipelineBehavior` | Runs all registered `FluentValidation.IValidator<TRequest>` validators before the handler. On failure it **short-circuits** and returns `Result.Failure(ErrorType.Validation)` — it does not throw, keeping validation a first-class domain outcome. |
| `LoggingPipelineBehavior` | Structured start/completion/failure logs including `RequestName`, `UserId`, `CorrelationId`, and the exception when one escapes. It never swallows the exception. |
| `TransactionPipelineBehavior` | Wraps state-mutating `ICommand` executions in one explicit `ITransactionManager` transaction so aggregate changes, interceptor-appended audit rows, and future outbox rows commit together. A failed `Result` or an exception disposes the scope without committing, which rolls back. Queries bypass it. |
| `QueryCachingPipelineBehavior` | Intercepts queries implementing `ICacheableQuery` and serves the cached `Result<TResponse>` when present, writing back on a miss. Target design. |

## Design notes

- Validation errors are returned as `Result`, so the API maps them to HTTP 400 via the same
  `ResultExtensions` path used for every other failure — no special-casing.
- The transaction behavior forwards the `CancellationToken` and relies on
  `await using`/`IAsyncDisposable` to roll back when it does not commit.
- Behaviors are generic and must remain infrastructure-agnostic (no EF Core / ASP.NET types).
- Audit writes are intentionally **not** a pipeline behavior: the EF interceptor sees the tracked
  entities and can therefore capture domain events and old/new values at save time.

## Contents

| File | Purpose |
| --- | --- |
| `ValidationPipelineBehavior.cs` | Runs every registered `IValidator<TRequest>` before the handler; on failure it short-circuits and returns `Result.Failure(Error.Validation(code, messages))` using the first FluentValidation error code, so validation stays a domain outcome instead of an exception. |
| `LoggingPipelineBehavior.cs` | Logs the request name, caller id, and correlation id around every request and rethrows failures. |
| `TransactionPipelineBehavior.cs` | Begins an explicit transaction for `ICommand`/`ICommand<TResponse>` requests, commits non-failure results, and rolls back on failure or exception. |

## Implementation status

| Behavior | State |
| --- | --- |
| `ValidationPipelineBehavior` | ✅ Implemented (returns a failed `Result`, never throws) |
| `LoggingPipelineBehavior` | ✅ Implemented |
| `TransactionPipelineBehavior` | ✅ Implemented |
| `QueryCachingPipelineBehavior` | ❌ Not implemented |

The caching behavior depends on abstractions that are not wired up yet (`ICacheableQuery`,
`ICacheService`); see [`../Abstractions/README.md`](../Abstractions/README.md).

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Added and registered `LoggingPipelineBehavior` and `TransactionPipelineBehavior` for Module 09; documented the actual registration order and rollback semantics. |
| 2026-10-19 | opencode | Recorded `ValidationPipelineBehavior` as ✅ implemented with a Contents row (the README previously reported it as not implemented). |
