# `src/widgets/movement-inbox` — Inbox / Outbox Queue

> **Status:** Docs only — Module 05 API live; code pending.

## Purpose

Tabbed department inbox/outbox table with routing action menu. Used by `pages/movements`.

Built on `shared/ui/data-table` with stable table ids `movements-inbox` / `movements-outbox`. The
API returns plain arrays, so pagination is client-side but uses the same pager and page-size control;
filters and tabs stay in URL search params, the column chooser persists visibility in `localStorage`,
and each row exposes a permission-filtered routing context menu mirrored by an overflow trigger.

## Composes

- `entities/movement`
- `features/movement-routing`

## URL

Respects `?tab=inbox|outbox&departmentId=` from the page.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Documented client-side pagination for the plain-array queues, stable table ids, `localStorage` column chooser, and row context menus. |
| 2026-10-25 | Cursor | Added movement-inbox widget docs for Module 05 queues. |
