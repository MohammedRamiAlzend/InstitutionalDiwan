# Infrastructure / BackgroundJobs

> **Status:** Target design — **no code yet**.

## Purpose

Recurring `BackgroundService` / Quartz.NET jobs that maintain institutional compliance across modules.
Jobs must be idempotent and safe to run concurrently with normal request traffic.

## Planned jobs

### `AuditChainIntegrityVerificationJob.cs` (Module 09)
Periodically walks `AUDIT_LOGS` **in order**, calling `auditLog.VerifyIntegrity(expectedPreviousHash)`
on each row (the domain method already recomputes the SHA-256 `EventHash` and checks the link to
`PreviousHash`). Writes an immutable pass/fail report into `AUDIT_INTEGRITY_CHECKS`.

### `DailyAuditCheckpointJob.cs` (Module 09)
Creates a daily `AuditCheckpoint` that seals the `TerminalHash` and `RecordCount` for the completed
range of log ids, establishing a trusted anchor for future integrity verification.

### `ExpiredDelegationAndAccessCleanupJob.cs` (Module 02)
Marks expired `UserDepartmentAssignment` records (`EndDate <= UtcNow`) inactive, and can also expire
`DocumentAccessGrant`s whose `ExpiresAt` has passed.

### `OverdueMovementEscalationJob.cs` (Modules 05 & 10)
Scans `DOCUMENT_MOVEMENT_STEPS` where `DueDate < UtcNow` and the step is still `SENT` or `RECEIVED`,
then enqueues high-priority overdue `Notification` alerts (via the outbox).

## Design notes

- Use the application `IDateTimeProvider`, never `DateTime.UtcNow`, so schedules and escalation
  cut-offs are testable.
- Record job runs (start, end, counts, failures) so operators can detect a silently stalled scheduler.
- Each job should be registered with a distinct, configurable cron/interval from `appsettings.json`.

## Implementation status

| Job | State |
| --- | --- |
| `AuditChainIntegrityVerificationJob` | ❌ Not implemented |
| `DailyAuditCheckpointJob` | ❌ Not implemented |
| `ExpiredDelegationAndAccessCleanupJob` | ❌ Not implemented |
| `OverdueMovementEscalationJob` | ❌ Not implemented |

The domain support they rely on **is** present: `AuditLog.VerifyIntegrity`, `AuditCheckpoint`,
`UserDepartmentAssignment.EndAssignment`, and `DocumentMovementStep.DueDate/StepStatus`.
