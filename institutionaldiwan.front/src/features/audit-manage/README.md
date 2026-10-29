# `src/features/audit-manage` — Checkpoints & Integrity Checks (Module 09)

> **Status:** Implemented — checkpoint dialog, integrity-check dialog with in-place pass/fail result, both behind `AUDIT_MANAGE`.

## Purpose

Write side of the audit compliance API: seal checkpoints and run integrity verification. Requires
`AUDIT_MANAGE`. Search remains in `entities/audit-log`.

## Contents

| File | Purpose |
| --- | --- |
| `api/createCheckpoint.ts` | `POST /api/audit-logs/checkpoints`; invalidates `auditLogKeys.all` |
| `api/runIntegrityCheck.ts` | `POST /api/audit-logs/integrity-checks`; invalidates `auditLogKeys.all` |
| `model/types.ts` | `CreateCheckpointRequest`, `RunIntegrityCheckRequest` |
| `model/checkpoint.schema.ts` | Optional positive-integer `toAuditLogId` |
| `model/integrity.schema.ts` | Optional `startAuditLogId` / `endAuditLogId` + end-before-start rejection |
| `ui/CreateCheckpointButton.tsx` | Toolbar button owning the dialog open state |
| `ui/CreateCheckpointDialog.tsx` | Optional `toAuditLogId` (prefillable), seals to the latest log when empty |
| `ui/RunIntegrityCheckDialog.tsx` | Optional start/end range, renders the pass/fail body in place |
| `index.ts` | Public barrel |

## Semantics

- Empty checkpoint range → `409 Audit.NothingToCheckpoint` (or `Audit.ChainBroken`) — surfaced
  through the Arabic catalog from `shared/api/errorMessages`.
- Integrity RPC: **HTTP 200** on success path; if `isValid === false`, show a danger panel with
  `firstCorruptedLogId` / `failureDetails` (do not toast as transport failure). The dialog keeps the
  result visible and points to the integrity report list.
- Both dialogs render a form-level `role="alert"` banner for transport errors and a sonner toast on
  success.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Added audit-manage feature docs for checkpoints and integrity checks. |
| 2026-10-29 | Cursor | Implemented both mutations and dialogs, the Arabic 409 handling, and the in-dialog integrity result panel. |
