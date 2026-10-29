# `src/widgets/audit-log-table` — Audit Search & Integrity Toolbar

> **Status:** Implemented — `AuditLogTable` (search grid) and `AuditIntegrityTable` (report list).

## Purpose

Filterable audit grid (hash columns LTR) plus toolbar for checkpoint / integrity actions when the
caller has `AUDIT_MANAGE`. Used by `pages/audit`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/AuditLogTable.tsx` | Paged audit search with module/action filters, date range, search, sort, and hash columns |
| `ui/AuditIntegrityTable.tsx` | Paged integrity report list with range, verdict, and corrupted-log columns |
| `index.ts` | Public barrel |

## `AuditLogTable`

Built on `shared/ui/data-table` with stable table id `audit-logs`: server-side paging, filters,
search, and sort stay in URL search params (`moduleName`, `actionTypeId`, `fromUtc`, `toUtc` are the
filter keys; sort keys `occurredAt` / `module` / `action` / `entity` map to `sortBy`). Date inputs are
day boundaries converted to `…T00:00:00Z` / `…T23:59:59Z`. `auditLogId` and `occurredAtUtc` are
identifying columns and can never be hidden.

Visible columns: log id, occurred-at, module (Arabic badge from `AUDIT_MODULES`), action and entity
type (lookup labels), entity id, and the truncated `eventHash` pill. Hidden by default but
chooseable: `previousHash`, actor user/department, related document, IP, and correlation id.

Menus:

- toolbar — checkpoint and «فحص السلامة» buttons when the caller has `AUDIT_MANAGE`;
- row — copy hash / id / entity always, checkpoint-up-to-this-log and check-from-this-log when manager;
- header and empty area — refresh, plus the manager actions.

## `AuditIntegrityTable`

Own `useTableState()` and table id `audit-integrity-reports` (client-side paging over the API's
newest-first list). Columns: check id, executed-at, `#start – #end` range, `IntegrityStatusBadge`,
first corrupted log id, plus hidden `failureDetails` (LTR, clamped) and `executedByUserId`. Row menu
copies the check id and the corrupted id and offers «إعادة فحص هذا النطاق» to managers; toolbar and
header expose run-integrity for managers.

## Composes

- `entities/audit-log`
- `features/audit-manage`

## Notes

Avoid polling — each search appends a self-audit row; the entity hooks disable focus refetch and use
a five-minute `staleTime`, and refresh is explicit. Chain-affecting actions run through a
confirmation dialog.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Documented the shared data-grid contract: table id `audit-logs`, URL filters/paging, `localStorage` column chooser, and row/header context menus. |
| 2026-10-25 | Cursor | Added audit-log-table widget docs for Module 09. |
| 2026-10-29 | Cursor | Implemented both tables with URL-driven filters, permission-filtered menus, and the copy/hash row actions. |
