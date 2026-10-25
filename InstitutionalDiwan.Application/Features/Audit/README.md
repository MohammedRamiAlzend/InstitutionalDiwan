# Application / Features / Audit (Module 09)

> **Status:** Implemented — filtered search, checkpoints, integrity checks, and integrity reports.

## Purpose

The audit slice is the read/maintenance side of the tamper-evident chain. Ordinary audit rows are
written automatically by `TamperEvidentAuditInterceptor`; the application layer only appends explicit
self-audit rows for sensitive audit operations and exposes the compliance API.

## Components

| File | Responsibility |
| --- | --- |
| `AuditLogAppender.cs` | Appends one entry to the current end of the chain using `IRequestContext` (IP + correlation id) and `IDateTimeProvider`. It adds the row to the same unit of work but never calls `SaveChanges`, so the caller commits it atomically. |
| `GetAuditLogsQuery.cs` / `GetAuditLogsQueryHandler.cs` | Paged search by module, action type, entity type/id, actor, department, related document, correlation id, IP, time window, and free text; sortable by occurred time/module/action/entity. Every search appends a `View` self-audit row describing the caller and filters. |
| `CreateAuditCheckpointCommand.cs` / handler | Finds the last checkpoint, reads every entry after it (or up to `ToAuditLogId`), verifies each hash and link, then creates an `AuditCheckpoint` sealing the terminal hash and record count. An empty range returns `Audit.NothingToCheckpoint`; a damaged chain returns the verification error and writes nothing. |
| `RunAuditIntegrityCheckCommand.cs` / handler | Anchors the range on the previous row's `EventHash` (or genesis), recomputes each SHA-256, and persists either a pass or a failure report with the first corrupted id. A broken chain is still a **successful** command with `isValid = false`, so monitoring can read it. |
| `GetAuditIntegrityReportsQuery.cs` / handler | Newest-first paged list of persisted integrity reports. |
| `AuditResponses.cs` | Response DTOs for audit entries, checkpoints, and integrity reports. |

## Semantics worth knowing

- **Reads are audited.** `GET /api/audit-logs` appends one row per call, so repeated searches grow the
  chain by design.
- **Checkpoints verify before sealing.** A broken chain can never be sealed as valid.
- **Integrity failure is HTTP 200.** The command result is successful; the response body says
  `isValid = false` and names the first corrupted log id.
- **Append-only.** There is no update/delete command and the repository exposes none.
- **One successor per row.** Appends are serialized by a transaction-scoped application lock, so
  concurrent requests and the scheduled jobs extend the chain one at a time. A unique index on
  `PreviousHash` remains the last line of defence against a fork.
- **Chain reset is genesis.** The first row links to `AuditLog.GenesisHash` (64 zeroes).
- **Timestamp precision.** `OccurredAtUtc` is normalized to UTC milliseconds before hashing so the
  value read back from `datetime2(3)` verifies unchanged.

## HTTP surface

| Route | Permission | Behavior |
| --- | --- | --- |
| `GET /api/audit-logs` | `AUDIT_VIEW` | Filtered, paged search + self-audit. |
| `POST /api/audit-logs/checkpoints` | `AUDIT_MANAGE` | Seal new entries into a checkpoint. |
| `POST /api/audit-logs/integrity-checks` | `AUDIT_MANAGE` | Verify a range and persist the report. |
| `GET /api/audit-logs/integrity-checks` | `AUDIT_VIEW` | List persisted reports. |

## Maintenance

Two hosted jobs call these use cases on a schedule (`AuditJobs` configuration):

- `AuditChainIntegrityVerificationJob` every 24 hours;
- `DailyAuditCheckpointJob` every 24 hours, offset by five minutes.

## Tests

`AuditDomainTests` covers the hash chain (genesis, tamper detection, broken link, UTC normalization);
`AuditHandlerTests` covers search self-audit, checkpoint verify-before-seal, empty-range no-ops, and
pass/fail integrity reports; `AuditLogRepositoryTests` covers filtering/range reads against SQLite;
`TamperEvidentAuditInterceptorTests` covers event projection, redaction, and request-only writes.
