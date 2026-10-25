# Infrastructure / Persistence / Transactions

> **Status:** Implemented — `EfTransactionManager` implements `ITransactionManager`.

## Purpose

Give the Application pipeline an infrastructure-agnostic way to wrap a command in one explicit
database transaction, so the aggregate change, interceptor-appended audit rows, and any dependent
rows commit or roll back together.

## Component

### `EfTransactionManager.cs` (`: ITransactionManager`)
- `BeginTransactionAsync` starts an EF Core transaction, or returns a no-op scope if a transaction is
  already open (for example a handler that deliberately joined one).
- `EfTransactionScope.CommitAsync` commits and marks the scope complete.
- Disposing the scope without committing rolls back, so a failed `Result` or an exception leaves no
  partial write.

Registered as a scoped service by `AddInfrastructure`; `TransactionPipelineBehavior` consumes it for
`ICommand` requests only.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Added `EfTransactionManager` and its rollback-on-dispose scope for Module 09. |
