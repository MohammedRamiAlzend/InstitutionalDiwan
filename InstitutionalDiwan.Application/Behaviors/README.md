# Application / Behaviors

> **Status:** Target design — **no behaviors implemented yet**.

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

## Implementation status

| Behavior | State |
| --- | --- |
| `ValidationPipelineBehavior` | ❌ Not implemented |
| `LoggingPipelineBehavior` | ❌ Not implemented |
| `TransactionPipelineBehavior` | ❌ Not implemented |
| `QueryCachingPipelineBehavior` | ❌ Not implemented |

None of the abstractions they depend on (`IUnitOfWork`, `ICacheableQuery`, `IUserContext`) are wired
up yet; see [`../Abstractions/README.md`](../Abstractions/README.md).
