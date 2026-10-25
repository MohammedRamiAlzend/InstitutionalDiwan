# `src/pages/movements` — Department Inbox / Outbox (`/movements`)

> **Status:** Docs only — Module 05 API live; page code pending.

## Purpose

Department routing queues: inbox and outbox for the caller's primary department (or an in-scope
`departmentId`). Hosts routing action menus via `features/movement-routing`.

## Routes & URL state

- `/movements?tab=inbox|outbox&departmentId=`
- Optional search params for free-text filter when the widget supports it.

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
| 2026-10-25 | Cursor | Added movements page slice docs mapped to live Module 05 inbox/outbox APIs. |
