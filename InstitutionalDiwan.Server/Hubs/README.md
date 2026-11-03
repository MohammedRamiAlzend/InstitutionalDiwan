# `InstitutionalDiwan.Server/Hubs` — SignalR Hubs

> **Status:** Implemented (Module 10, 2026-11-03).

## Purpose

Realtime delivery of notification alerts to signed-in users. The hub accepts no client calls: the database stays the source of truth and the push is only an alert.

## Contents

| File | Purpose |
| --- | --- |
| `NotificationsHub.cs` | `[Authorize]` hub at `/hubs/notifications`. Adds each connection to the group `user:{userId}` and aborts it when the access token expires. Pushes the `notificationReceived` event. |
| `SignalRRealtimeNotifier.cs` | Implements the Application `IRealtimeNotifier` over `IHubContext<NotificationsHub>`; the outbox processor calls it. |

## Notes

- Browsers cannot set headers on a WebSocket upgrade, so `AuthenticationExtensions` reads the token from the `access_token` query string, for `/hubs` paths only.
- Single instance only: there is no backplane. A multi-instance deployment needs Redis or Azure SignalR and a single outbox processor.

## Implementation status

| Item | State |
| --- | --- |
| Hub, group per user, token expiry close, notifier | ✅ Implemented; verified against the live API with a raw WebSocket client (negotiate, handshake, push received) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | Claude | Added the notifications hub and its `IRealtimeNotifier` implementation (Module 10). |
