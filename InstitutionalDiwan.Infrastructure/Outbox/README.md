# Infrastructure / Outbox

> **Status:** Target design — **no code yet**.

## Purpose

Implements the **Transactional Outbox pattern** for Module 10 (Notifications & Outbox), so that
sending a notification is reliable even if email/SMS/SignalR is momentarily unavailable.

## Planned components

### `OutboxProcessorJob.cs`
Polls `NOTIFICATION_OUTBOX` for rows where `Status == "PENDING"` or
(`Status == "FAILED"` and `NextRetryAt <= UtcNow`), then:
- dispatches across the configured channel (`IN_APP` via SignalR, `EMAIL` via SMTP, or `SMS`/webhook);
- calls `outbox.MarkProcessed()` on success (→ `Status = SENT`);
- calls `outbox.RecordFailure(error, nextRetryAt)` with **exponential backoff** on failure, transitioning
  to `DEAD_LETTER` once `RetryCount` reaches the maximum.

These transitions already exist on the domain entity `NotificationOutbox`
(`MarkProcessed`, `RecordFailure(error, nextRetryAt, maxRetries = 5)`).

### `SignalRNotificationHub.cs`
Real-time hub pushing instant inbox/movement alerts to connected department clerks.

### Domain-event dispatch
The outbox is fed from the interceptor in
[`../Persistence/Interceptors/`](../Persistence/Interceptors/README.md): domain events raised by
aggregates during `SaveChangesAsync` are serialized into outbox rows **in the same transaction**,
guaranteeing at-least-once delivery without a distributed transaction.

## Channel status values

`PENDING → SENT`, or `PENDING → FAILED → … → DEAD_LETTER`. `RetryCount`, `LastError`, `NextRetryAt`,
and `ProcessedAt` are all persisted columns.

## Implementation status

| Component | State |
| --- | --- |
| `OutboxProcessorJob` | ❌ Not implemented |
| `SignalRNotificationHub` | ❌ Not implemented |
| Domain-event → outbox writing | ❌ Not implemented (interceptor empty) |

The persistence side is ready: `NOTIFICATION_OUTBOX` (with `Status`, `RetryCount`, `NextRetryAt`,
`ProcessedAt` and the `IX_NOTIFICATION_OUTBOX_STATUS_RETRY` index) is already configured.
