# `src/pages/movements` — Department Inbox / Outbox (`/movements`)

> **Status:** Docs only — Module 05 API live; page code pending.

## Purpose

Department routing queues: inbox and outbox for the caller's primary department (or an in-scope
`departmentId`). Hosts routing action menus via `features/movement-routing`.

## Routes & URL state

- `/movements?tab=inbox|outbox&departmentId=&page=&pageSize=&search=`
- Filters, search, and pagination live in URL search params; the queues return plain arrays, so the
  page slices them client-side through the shared pager.

## Table contract

`widgets/movement-inbox` uses the shared data grid (table ids `movements-inbox` /
`movements-outbox`) with a column chooser persisted in `localStorage` and a permission-filtered
routing context menu on every row and in the header.

## Planned Contents

| File | Purpose |
| --- | --- |
| `ui/MovementsPage.tsx` | Thin page composing `widgets/movement-inbox`. |
| `index.ts` | Public barrel. |

## Permissions

Route guard: `DOCUMENT_VIEW`. Actions inside use `DOCUMENT_EDIT` and domain movement rules
(Central Diwan hub, receive gated on department registration).

## Implementation status

| Item | State |
| --- | --- |
| Slice docs | ✅ |
| Page code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Documented URL-driven filters/paging and the shared table/context-menu contract for the queues. |
| 2026-10-25 | Cursor | Added movements page slice docs mapped to live Module 05 inbox/outbox APIs. |
