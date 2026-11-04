# `src/features/notification-live` — Live Notification Connection

> **Status:** Implemented — SignalR connection, reconnect policy, toast alerts.

## Purpose

Keeps one SignalR connection to `/hubs/notifications` open for the signed-in user. The push is only
an alert: every alert, fresh connection and reconnect invalidates the notification queries, and the
database stays the source of truth. `@microsoft/signalr` is imported lazily so it never ships in the
login chunk.

## Contents

| File | Purpose |
| --- | --- |
| `model/useNotificationHub.ts` | Builds and tears down the connection for the current user (clean under `StrictMode`), reconnects with SignalR's own schedule then an exponential retry, de-duplicates pushes by `notificationId`, and shows a toast whose «فتح» action calls the supplied `onOpen`. |
| `model/connectionStore.ts` | Small zustand store of the connection state (`idle`, `connecting`, `connected`, `reconnecting`, `disconnected`) and the `useNotificationConnection` selector used by the bell and the unread polling fallback. |
| `index.ts` | Public barrel. |

## Conventions / Notes

- The token travels as the `access_token` query string through `accessTokenFactory`; never log the hub URL.
- The server closes the hub when the token expires; the next HTTP call then answers 401 and the HTTP client signs the user out.
- Mounted once per session by `app/router/NotificationLiveLayout`, which also supplies `onOpen` (mark read + navigate), because a feature may not import a sibling feature.
- Urgent notifications (priority 3) keep their toast until dismissed; others close after 6 seconds.

## Implementation status

| Item | State |
| --- | --- |
| Connection, reconnect, de-duplication, toast | ✅ |
| Connection indicator in the bell | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | Added the Module 10 live connection. |
