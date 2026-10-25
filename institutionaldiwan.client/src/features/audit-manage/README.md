# `src/features/audit-manage` — Checkpoints & Integrity Checks (Module 09)

> **Status:** Docs only — API live; code pending.

## Purpose

Write side of the audit compliance API: seal checkpoints and run integrity verification. Requires
`AUDIT_MANAGE`. Search remains in `entities/audit-log`.

## Planned Contents

| File | Purpose |
| --- | --- |
| `api/createCheckpoint.ts` | `POST /api/audit-logs/checkpoints` |
| `api/runIntegrityCheck.ts` | `POST /api/audit-logs/integrity-checks` |
| `model/checkpoint.schema.ts` | Optional `toAuditLogId` |
| `model/integrity.schema.ts` | Optional `startAuditLogId` / `endAuditLogId` |
| `ui/CreateCheckpointButton.tsx` | Confirm + mutate |
| `ui/RunIntegrityCheckDialog.tsx` | Range form; render pass/fail from body |
| `index.ts` | Public barrel |

## Semantics

- Empty checkpoint range → domain error (e.g. `Audit.NothingToCheckpoint`) — show `detail`.
- Integrity RPC: **HTTP 200** always on success path; if `isValid === false`, show danger banner with
  `firstCorruptedLogId` / `failureDetails` (do not toast as transport failure).
- Invalidate `auditLogKeys.integrity` (and optionally list) after success.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Added audit-manage feature docs for checkpoints and integrity checks. |
