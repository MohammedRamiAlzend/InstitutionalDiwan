# Infrastructure / Persistence / Interceptors

> **Status:** Partially implemented — `TamperEvidentAuditInterceptor` is live and registered on
> `DiwanDbContext`; the domain-event/outbox interceptor for Module 10 is still target design.

## Purpose

EF Core `SaveChangesInterceptor` implementations that hook into `DiwanDbContext.SaveChangesAsync` to
give persistence two cross-cutting behaviors: **domain-event dispatch** and **tamper-evident auditing**.

## Implemented component

### `TamperEvidentAuditInterceptor.cs`
Appends one or more immutable `AuditLog` rows to the same `SaveChanges` call as the business write,
so a business change and its audit trail commit or roll back together. The interceptor:

- reads the caller's `sub`/`dept_id` claims, client IP, and `X-Correlation-Id` from `HttpContext`;
- projects known domain events (`DepartmentCreated`, `DocumentRegistered`, `MovementSent`,
  `DocumentFileVersionUploaded`, `UserLoggedIn`, etc.) into precise audit rows with module, action,
  entity type, related document, and old/new JSON; `UserLoggedInEvent` is raised during the anonymous
  login request and is attributed to the authenticated user it describes, so logins are recorded;
- falls back to a generic old/new snapshot for tracked aggregate types that publish no event;
- fills `PreviousHash → EventHash` with a SHA-256 chain, including any explicit row already pending in
  the same save, and re-seals those pending rows onto the predecessor that is current once the chain
  lock is held;
- serializes appends with a transaction-scoped SQL Server application lock (`sp_getapplock` on
  `InstitutionalDiwan.AuditChain`), so concurrent requests and the scheduled jobs extend the chain one
  at a time; the head is read with `UPDLOCK, HOLDLOCK` because the database runs
  `READ_COMMITTED_SNAPSHOT`, where an ordinary `SELECT` can still see a stale snapshot;
- filters properties containing `password`, `secret`, `token`, `salt`, `securitystamp`, or
  `concurrencystamp`;
- skips audit/notification/lookup entities entirely;
- performs **no automatic** audit writes when there is no `HttpContext` (startup seeding, migrations,
  ordinary background writes) and clears the domain events so they do not leak across saves. Explicit
  rows appended by a job through `AuditLogAppender` **are** still chained and serialized.

Explicit audit rows appended by the application (`AuditLogAppender`) are therefore committed in the
same save as the operation they describe, after re-sealing onto the locked head.

This interceptor audits **entity changes**. Reads, rejected requests, and other actions that change
no entity are covered by the server's `AuditActionMiddleware`, which appends one `Operation` row per
authenticated controller action through `IAuditActionRecorder` (an isolated scope, committed
separately). The two layers share the same hash chain and differ only in what they describe.

## Planned component

### `PublishDomainEventsInterceptor.cs` / `InsertOutboxMessagesInterceptor.cs` (Module 10)
Extracts every raised `IDomainEvent` from tracked aggregate roots and serializes the notification
work into `NOTIFICATION_OUTBOX` inside the same database transaction. This is separate from the audit
chain: audit rows are append-only compliance records, while outbox rows are retryable work items.

## Domain support that already exists

- `AggregateRoot<TId>.DomainEvents` / `ClearDomainEvents()` — the source of truth for pending events.
- `AuditLog.Record(...)` computes the hash; `IAuditLogRepository.GetLatestAsync()` provides the previous
  `EventHash` needed to chain the next entry.
- The `IX_AUDIT_LOGS_OCCURRED_AT` / `IX_AUDIT_LOGS_DOC` indexes back common audit queries.

## Design notes

- The audit interceptor must be **append-only**; never update or delete an `AuditLog` row.
- Hash chaining is order-sensitive — take the previous hash from the database, not from an in-memory
  cache. The transaction-scoped application lock is what serializes writers; the unique index on
  `PreviousHash` remains the last line of defence.
- The lock is released by the transaction that holds it, so the interceptor opens its own transaction
  only when the caller does not already have one (queries such as the audited audit-log search) and
  commits it in `SavedChanges`; commands already carry the pipeline transaction.
- Both interceptors must run inside the same transaction as the business change.
- Sensitive fields are redacted by name, not value, so passwords and tokens never reach the log even
  when an aggregate is audited through the generic fallback.

## Implementation status

| Interceptor | State |
| --- | --- |
| `PublishDomainEventsInterceptor` / `InsertOutboxMessagesInterceptor` | ❌ Not implemented |
| `TamperEvidentAuditInterceptor` | ✅ Implemented and registered as a singleton in `AddInfrastructure` |

The audit interceptor reads `IHttpContextAccessor` directly and is registered as a singleton to avoid
scoped-interceptor lifetime issues with `AddDbContext`.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-26 | Qoder | Mapped `UserLoggedInEvent` to a `Login` audit row attributed to the user who logged in (login requests are still anonymous while the interceptor runs), and documented the split between entity audit rows and action-level `Operation` rows. |
| 2026-10-25 | Buffy | Serialized audit-chain appends with a transaction-scoped `sp_getapplock` plus a locking head read (`UPDLOCK, HOLDLOCK`) after concurrent reads/writes collided on `UX_AUDIT_LOGS_PREVIOUS_HASH`; added `AuditLog.Reseal` so explicit rows are re-chained under the lock, and made jobs' explicit rows go through the same path. |
