# Infrastructure / Persistence / Interceptors

> **Status:** Target design — **no code yet**.

## Purpose

EF Core `SaveChangesInterceptor` implementations that hook into `DiwanDbContext.SaveChangesAsync` to
give persistence two cross-cutting behaviors: **domain-event dispatch** and **tamper-evident auditing**.

## Planned components

### `PublishDomainEventsInterceptor.cs` (or `InsertOutboxMessagesInterceptor.cs`)
- Inspects tracked aggregate roots via `ChangeTracker.Entries<AggregateRoot>()`.
- Extracts every raised `IDomainEvent` from `entity.DomainEvents`, then calls `entity.ClearDomainEvents()`.
- Either dispatches them through MediatR `IPublisher`, or serializes them into
  `NOTIFICATION_OUTBOX` / `AUDIT_LOGS` **within the same database transaction** (the transactional-outbox
  approach — see [`../../Outbox/README.md`](../../Outbox/README.md)).

### `TamperEvidentAuditInterceptor.cs`
Automatically records immutable `AuditLog` entries with SHA-256 hash chaining
(`PreviousHash → EventHash`) whenever sensitive aggregates are created or mutated:
`Document`, `DocumentNumber`, `DocumentMovement`, `User`, `Role`, `DocumentAccessGrant`.

## Domain support that already exists

- `AggregateRoot<TId>.DomainEvents` / `ClearDomainEvents()` — the source of truth for pending events.
- `AuditLog.Record(...)` computes the hash; `IAuditLogRepository.GetLatestAsync()` provides the previous
  `EventHash` needed to chain the next entry.
- The `IX_AUDIT_LOGS_OCCURRED_AT` / `IX_AUDIT_LOGS_DOC` indexes back common audit queries.

## Design notes

- The audit interceptor must be **append-only**; never update or delete an `AuditLog` row.
- Hash chaining is order-sensitive — take the previous hash from the database, not from an in-memory
  cache, and let the database serialize appends.
- Both interceptors must be registered so they run inside the same transaction as the business change.

## Implementation status

| Interceptor | State |
| --- | --- |
| `PublishDomainEventsInterceptor` / `InsertOutboxMessagesInterceptor` | ❌ Not implemented |
| `TamperEvidentAuditInterceptor` | ❌ Not implemented |

No interceptor is registered on `DiwanDbContext` today, so domain events raised by aggregates are
currently never dispatched and no audit rows are produced.
