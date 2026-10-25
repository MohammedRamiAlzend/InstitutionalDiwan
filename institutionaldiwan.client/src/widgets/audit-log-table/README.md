# `src/widgets/audit-log-table` — Audit Search & Integrity Toolbar

> **Status:** Docs only — Module 09 API live; code pending.

## Purpose

Filterable audit grid (hash columns LTR) plus toolbar for checkpoint / integrity actions when the
caller has `AUDIT_MANAGE`. Used by `pages/audit`.

## Composes

- `entities/audit-log`
- `features/audit-manage`

## Notes

Debounce filter changes; avoid polling — each search appends a self-audit row.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Added audit-log-table widget docs for Module 09. |
