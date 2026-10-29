# `src/entities/audit-log` — Audit Log Read Model (Module 09)

> **Status:** Implemented — paged audit search, integrity report reads, module labels, and the hash/integrity badges.

## Purpose

Paged audit search and integrity report reads. Mutations (checkpoint / run check) live in
`features/audit-manage`.

## Contents

| File | Purpose |
| --- | --- |
| `api/listAuditLogs.ts` | `GET /api/audit-logs` (side-effecting on server: each read appends a View self-audit row) |
| `api/listIntegrityReports.ts` | `GET /api/audit-logs/integrity-checks` (only `page` / `pageSize`; newest first) |
| `model/types.ts` | `AuditLogResponse`, `AuditIntegrityCheckResponse`, `AuditCheckpointResponse`, `AuditLogListParams`, `AuditIntegrityListParams`, `AuditLogSortKey` |
| `model/constants.ts` | `AUDIT_MODULES` Arabic labels (`IDENTITY`…`AUDIT`, interceptor-only `ARCHIVING`/`RELATIONS`, fallback `LOOKUPS`) + `auditModuleLabel()` |
| `model/queries.ts` | `auditLogKeys` + `useAuditLogs` / `useAuditIntegrityReports` |
| `ui/IntegrityStatusBadge.tsx` | Pass / fail badge for `isValid` («سليم» / «مخترق») |
| `ui/HashChainPill.tsx` | Truncated `dir="ltr"` monospace hash display with full value in `title` |
| `index.ts` | Public barrel |

## Query keys

```ts
export const auditLogKeys = {
  all: ['audit-logs'] as const,
  list: (params: AuditLogListParams) => [...auditLogKeys.all, 'list', params] as const,
  integrity: (params: AuditIntegrityListParams) => [...auditLogKeys.all, 'integrity', params] as const,
};
```

Both hooks use `staleTime` of five minutes, `refetchOnWindowFocus: false`, and keep the previous
page while the next one loads. Invalidate `auditLogKeys.all` after a checkpoint or a check.

## Labels

The action and entity type columns resolve their Arabic labels from the `audit-action-types` and
`audit-entity-types` lookups (`entities/lookup`), which only require authentication. `moduleName`
is a free string from the API, so it is mapped through `AUDIT_MODULES` with a raw-value fallback.

## Notes

Prefer longer `staleTime` and explicit user-driven refetch — each logs list call appends a chain row.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Added audit-log entity slice docs for Module 09 search and integrity reports. |
| 2026-10-29 | Cursor | Implemented the reads, query keys, Arabic module labels, and the hash/integrity badges; added the `LOOKUPS` fallback module after browser verification. |
