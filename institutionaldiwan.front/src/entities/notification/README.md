# `src/entities/notification` — Notification Reads

> **Status:** Implemented — typed reads, query keys and presentational pieces for Module 10.

## Purpose

Everything the front end reads about the caller's own notifications: the inbox page, the unread
count (total and per type) and the small UI pieces shared by the bell and the inbox page. Writes
(read, read-all, dismiss) live in `features/notification-manage`; the live SignalR connection lives
in `features/notification-live`.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | Wire types: `NotificationResponse`, `UnreadCountResponse` (`count` + `byType`), `NotificationListParams`, `NotificationHubPayload` (the push, an alert only). |
| `api/notificationApi.ts` | `GET /api/notifications` (search, sort, unread and type filters, paging) and `GET /api/notifications/unread-count`. |
| `model/queries.ts` | `notificationKeys` (every key carries the user id so accounts never share a cache), `useNotifications` and `useUnreadCount` (polls every minute only while the live connection is down). |
| `ui/NotificationItem.tsx` | One row for the bell and the inbox: type icon, unread dot, title, body, type label, time, and an `actions` slot. |
| `ui/NotificationPriorityBadge.tsx` | Warning/danger badge for high and urgent notifications; renders nothing for normal ones. |
| `ui/NotificationTypeIcon.tsx` | Decorative icon per notification type (the type name is always shown as text too). |
| `index.ts` | Public barrel. |

## Conventions / Notes

- Read and dismiss use `recipientId`, never `notificationId`; `notificationId` only de-duplicates pushes.
- `actionUrl` is a relative app path; always pass it through `safeRelativeUrl` before navigating.
- No permission code is needed: the server scopes every route to the caller.

## Implementation status

| Item | State |
| --- | --- |
| List and unread-count reads | ✅ |
| Row, priority badge, type icon | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | Added the Module 10 notification entity (plan `plans/module-10-notifications-frontend.md`). |
