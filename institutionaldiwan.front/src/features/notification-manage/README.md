# `src/features/notification-manage` — Notification Writes

> **Status:** Implemented — mark read, mark all read, dismiss.

## Purpose

The three Module 10 writes. Each one updates the cached inbox and the unread badge optimistically,
rolls back on failure, and always re-reads the server state afterwards.

## Contents

| File | Purpose |
| --- | --- |
| `api/notificationMutations.ts` | `useMarkReadMutation` (`POST /{recipientId}/read`), `useDismissMutation` (`POST /{recipientId}/dismiss`) and `useMarkAllReadMutation` (`POST /read-all`), with snapshot/rollback and invalidation of `notificationKeys.all(userId)` on settle. |
| `index.ts` | Public barrel (also exports `NotificationTarget`, the row facts a caller must pass so the badge can move early). |

## Conventions / Notes

- The calls are idempotent on the server, so a repeated click is harmless.
- A row that is not the caller's answers 404 `Notifications.RecipientNotFound`; render it with `toArabicErrorMessage`.

## Implementation status

| Item | State |
| --- | --- |
| Mark read, dismiss, read-all | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | Added the Module 10 notification writes. |
