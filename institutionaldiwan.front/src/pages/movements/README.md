# `src/pages/movements` — Department Inbox / Outbox (`/movements`)

> **Status:** Implemented — the «الحركات» navigation item is live.

## Purpose

Department routing queues: inbox and outbox for the caller's primary department. Hosts routing
action menus via `features/movement-routing`.

## Routes & URL state

- `/movements?tab=inbox|outbox&page=&pageSize=&search=&sort=&statusId=&movementTypeId=`
- The tab lives in the URL (`tab=outbox`; inbox is the default and clears the param). Search,
  filters, sorting, and pagination are owned by `useTableState`, while the queues return plain
  arrays that the widget slices client-side through the shared pager.
- There is no department selector: both queues describe the caller's primary department because the
  session does not expose a department scope and the API has no scoped queue parameter.

## Table contract

`widgets/movement-inbox` uses the shared data grid (table ids `movements-inbox` /
`movements-outbox`) with a column chooser persisted in `localStorage` and a permission-filtered
routing context menu on every row and in the header.

## Contents

| File | Purpose |
| --- | --- |
| `ui/MovementsPage.tsx` | Thin page: header, inbox/outbox tablist, composes `widgets/movement-inbox`. |
| `index.ts` | Public barrel. |

## Permissions

Route guard: `DOCUMENT_VIEW`. Actions inside use `DOCUMENT_EDIT` and domain movement rules
(Central Diwan hub, receive gated on department registration).

## Implementation status

| Item | State |
| --- | --- |
| Slice docs | ✅ |
| Page code | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | Cursor | Implemented the page and wired it into the router and sidebar; removed the unbuilt `departmentId` parameter from the URL contract. |
| 2026-10-25 | Buffy | Documented URL-driven filters/paging and the shared table/context-menu contract for the queues. |
| 2026-10-25 | Cursor | Added movements page slice docs mapped to live Module 05 inbox/outbox APIs. |
