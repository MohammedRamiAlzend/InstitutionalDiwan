# `src/entities/audit-log` — Audit Log Read Model (Module 09)

> **Status:** Docs only — API live; code pending.

## Purpose

Paged audit search and integrity report reads. Mutations (checkpoint / run check) live in
`features/audit-manage`.

## Planned Contents

| File | Purpose |
| --- | --- |
| `api/listAuditLogs.ts` | `GET /api/audit-logs` (side-effecting on server) |
| `api/listIntegrityReports.ts` | `GET /api/audit-logs/integrity-checks` |
| `model/types.ts` | Matches `AuditLogResponse` / `AuditIntegrityCheckResponse` in `knowledge.md` |
| `model/queries.ts` | `auditLogKeys` + hooks |
| `ui/IntegrityStatusBadge.tsx` | Pass / fail for `isValid` |
| `ui/HashChainPill.tsx` | Truncated `dir="ltr"` hash display |
| `index.ts` | Public barrel |

## Query keys (contract)

```ts
export const auditLogKeys = {
  all: ['audit-logs'] as const,
  list: (params: object) => [...auditLogKeys.all, 'list', params] as const,
  integrity: (params: object) => [...auditLogKeys.all, 'integrity', params] as const,
};
```

## Notes

Prefer longer `staleTime` and explicit user-driven refetch — each logs list call appends a chain row.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Added audit-log entity slice docs for Module 09 search and integrity reports. |
