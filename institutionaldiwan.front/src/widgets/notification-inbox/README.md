# `src/widgets/notification-inbox` — Notification Inbox Grid

> **Status:** Implemented — server-side search, sort, paging and filters on the shared grid.

## Purpose

The full inbox on `/notifications`. Search, sort (`deliveredAt`, `priority`, `type`, `isRead`),
paging and the unread and type filters all live in the URL and are executed by the server; the type
filter shows the unread count of each type from `unread-count.byType`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/NotificationInbox.tsx` | The `DataTable` (`tableId="notifications"`, column preference `diwan.table.notifications.columns.v1`) with the filter bar, «قراءة الكل» toolbar action, and a permission-free context menu (open, mark read, dismiss, copy link). |
| `index.ts` | Public barrel. |

## Conventions / Notes

- Opening a row marks it read and then navigates to its `actionUrl` when it passes `safeRelativeUrl`.
- No «mark as unread» exists in the API, so the menu has none.

## Implementation status

| Item | State |
| --- | --- |
| Grid, filters, row/header/empty menus | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب5): the inbox columns (`headerKey`), filters, row/header menus, caption, search, copy toasts and empty states render from `notification.inbox.*` via `<Label>`/`t()`/`resolveLabel`. |
| 2026-11-04 | Claude | Added the Module 10 inbox grid. |
