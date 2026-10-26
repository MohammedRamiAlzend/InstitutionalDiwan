# `src/widgets/audit-log-table` — Audit Search & Integrity Toolbar

> **Status:** Docs only — Module 09 API live; code pending.

## Purpose

Filterable audit grid (hash columns LTR) plus toolbar for checkpoint / integrity actions when the
caller has `AUDIT_MANAGE`. Used by `pages/audit`.

Built on `shared/ui/data-table` with stable table id `audit-logs`: server-side filters/sort/paging
stay in URL search params, the column chooser persists visibility in `localStorage`, and every row,
header, and empty area exposes a permission-filtered context menu. `auditLogId` / `occurredAtUtc`
are identifying columns and can never be hidden.

## Composes

- `entities/audit-log`
- `features/audit-manage`

## Notes

Debounce filter changes; avoid polling — each search appends a self-audit row. Row actions
(checkpoint, verify, copy hash/id) live in the row context menu and mirror the toolbar; destructive
or chain-affecting actions require confirmation.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Documented the shared data-grid contract: table id `audit-logs`, URL filters/paging, `localStorage` column chooser, and row/header context menus. |
| 2026-10-25 | Cursor | Added audit-log-table widget docs for Module 09. |
