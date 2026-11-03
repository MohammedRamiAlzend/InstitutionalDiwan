# `Application/Abstractions/Notifications`

> **Status:** Implemented (2026-11-03).

## Contents

| File | Purpose |
| --- | --- |
| `INotificationPublisher.cs` | Stages a movement or document notification inside the caller's unit of work. |
| `INotificationRecipientDirectory.cs` | Department users and the confidentiality filter; implemented in Infrastructure. |
| `IRealtimeNotifier.cs` | Realtime alert contract and the `NotificationPush` payload; implemented in the Server with SignalR. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | Claude | Added the Module 10 contracts. |
