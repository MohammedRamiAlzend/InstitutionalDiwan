# `src/pages/notifications` — `/notifications`

> **Status:** Implemented.

## Purpose

The route for the caller's own notification inbox. It needs no permission code; the server scopes
every notification route to the signed-in user.

## Contents

| File | Purpose |
| --- | --- |
| `ui/NotificationsPage.tsx` | Page header with the tutorial trigger and the inbox widget inside the `notifications` guide provider. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Route and page | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | Added the Module 10 notifications page. |
