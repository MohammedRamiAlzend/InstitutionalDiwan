# Infrastructure / BackgroundJobs

> **Status:** Partially implemented — the two Module 09 audit jobs are live; the delegation
> cleanup and movement escalation jobs remain target design.

## Purpose

Recurring `BackgroundService` jobs that maintain institutional compliance across modules. Jobs must
be idempotent and safe to run concurrently with normal request traffic.

## Implemented jobs

Both jobs derive from `PeriodicAuditJob`, which waits for startup seeding to finish, resolves an
actor account, runs one unit of work in its own DI scope, and logs rather than throws when a run
fails.

### `AuditChainIntegrityVerificationJob.cs` (Module 09)
Walks `AUDIT_LOGS` in order from the first id (or the requested range), calling
`auditLog.VerifyIntegrity(expectedPreviousHash)` on each row. The domain method recomputes the
SHA-256 `EventHash` and checks the link to `PreviousHash`. The run writes an immutable pass/fail
report into `AUDIT_INTEGRITY_CHECKS` and appends a self-audit entry for the check itself.

### `DailyAuditCheckpointJob.cs` (Module 09)
Creates a checkpoint that seals the `TerminalHash` and `RecordCount` for every log added since the
previous checkpoint. If no new logs exist the job logs a debug message and does nothing. The
checkpoint also appends a self-audit entry.

## Configuration

`AuditJobsOptions` binds the `AuditJobs` section:

```json
"AuditJobs": {
  "Enabled": true,
  "ActorUserId": null,
  "StartupDelaySeconds": 60,
  "IntegrityCheckIntervalHours": 24,
  "CheckpointIntervalHours": 24,
  "CheckpointOffsetMinutes": 5
}
```

`ActorUserId` is the account attributed to job-created checkpoints and reports. When omitted, the
job uses the first active user. Set it explicitly in production so maintenance rows are attributed
consistently.

## Planned jobs

### `ExpiredDelegationAndAccessCleanupJob.cs` (Module 02)
Marks expired `UserDepartmentAssignment` records (`EndDate <= UtcNow`) inactive, and can also expire
`DocumentAccessGrant`s whose `ExpiresAt` has passed.

### `OverdueMovementEscalationJob.cs` (Modules 05 & 10)
Scans `DOCUMENT_MOVEMENT_STEPS` where `DueDate < UtcNow` and the step is still `SENT` or `RECEIVED`,
then enqueues high-priority overdue `Notification` alerts (via the outbox).

## Design notes

- Prefer the application `IDateTimeProvider` for new jobs rather than `DateTime.UtcNow`, so schedules
  and escalation cut-offs stay testable.
- Record job runs (start, end, counts, failures) so operators can detect a silently stalled scheduler.
- Each job should be registered with a distinct, configurable interval from `appsettings.json`.
- Audit jobs run outside an HTTP request; the tamper-evident interceptor therefore skips their
  ordinary entity writes and only the explicit self-audit rows are appended.

## Implementation status

| Job | State |
| --- | --- |
| `AuditChainIntegrityVerificationJob` | ✅ Implemented |
| `DailyAuditCheckpointJob` | ✅ Implemented |
| `ExpiredDelegationAndAccessCleanupJob` | ❌ Not implemented |
| `OverdueMovementEscalationJob` | ❌ Not implemented |

The domain support the remaining jobs rely on **is** present:
`UserDepartmentAssignment.EndAssignment` and `DocumentMovementStep.DueDate/StepStatus`.
