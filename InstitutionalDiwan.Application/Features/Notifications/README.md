# `Application/Features/Notifications` — Module 10 Use Cases

> **Status:** Implemented (2026-11-03).

## Purpose

Staging notifications when business events happen, and the personal inbox use cases.

## Contents

| File | Purpose |
| --- | --- |
| `NotificationPublisher.cs` | `INotificationPublisher`: picks recipients per event type, drops the actor and anyone who may not know the document exists, fills the type template, and adds the notification with one IN_APP outbox row to the current unit of work. Never saves, never throws. |
| `NotificationResponse.cs` | Inbox row, unread count (total plus per-type `ByType`) and bulk-read responses. |
| `ListNotificationsQuery.cs` / `ListNotificationsQueryHandler.cs` | The caller's own paged inbox (unread and type filters, Arabic title/body search, sort by deliveredAt, priority, type or isRead; newest first by default). |
| `GetUnreadCountQuery.cs` | Unread, non-dismissed count for the bell. |
| `NotificationCommands.cs` | Mark read, dismiss and read-all, each scoped to the caller; another user's row is `Notifications.RecipientNotFound`. |

## Implementation status

| Item | State |
| --- | --- |
| Publisher, inbox queries, commands | ✅ Implemented and covered by `NotificationsTests` |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | The inbox query gained `Search`, `SortBy` and `SortDescending` (all passed through `PageRequest`), and `UnreadCountResponse` gained `ByType` (backed by `INotificationRepository.CountUnreadByTypeAsync`). Additive: no migration. |
| 2026-11-03 | Claude | Added the Module 10 use cases. |
