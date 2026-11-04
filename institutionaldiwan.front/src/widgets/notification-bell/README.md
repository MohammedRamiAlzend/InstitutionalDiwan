# `src/widgets/notification-bell` — Header Bell

> **Status:** Implemented — unread badge, live-connection indicator, latest notifications.

## Purpose

The bell in the app header: an unread badge (`99+` cap), a panel with the latest eight notifications
(open, mark read, dismiss, read all), a link to `/notifications`, and a line telling the user whether
the live connection is up. Mounted through `AppShell`'s `headerSlot` by `app/router/AppRouter`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/NotificationBell.tsx` | The button, badge and disclosure panel (closes on Escape or outside click) with loading, error + retry and empty states. |
| `index.ts` | Public barrel. |

## Conventions / Notes

- The panel list is fetched only while open; the badge comes from `useUnreadCount`.
- The accessible name carries the unread count (`الإشعارات، 3 غير مقروءة`).
- Not part of a page, so it has no tutorial-guide entry.

## Implementation status

| Item | State |
| --- | --- |
| Badge, panel, actions, connection line | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | Added the Module 10 notification bell. |
