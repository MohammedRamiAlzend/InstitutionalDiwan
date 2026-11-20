# `Application/Abstractions/Notifications`

> **Status:** Implemented (2026-11-03).

## Contents

| File | Purpose |
| --- | --- |
| `INotificationPublisher.cs` | Stages a movement, document, or department notification inside the caller's unit of work. `PublishToDepartmentAsync` notifies that department's users and still drops the actor. |
| `INotificationRecipientDirectory.cs` | Department users and the confidentiality filter; implemented in Infrastructure. |
| `IRealtimeNotifier.cs` | Realtime alert contract and the `NotificationPush` payload; implemented in the Server with SignalR. |
| `IDriveNotificationPublisher.cs` | Drive (Module 11) notification contract: `PublishAsync(typeId, recipientUserIds, itemTitle, actorUserId, requesterName?, actionUrl?)` stages an inbox notification to **named users** (deletion approver/requester, grantee) with no document confidentiality gate, inside the caller's unit of work; never saves, never throws. Implemented by `Application/Features/Drive/DriveNotificationPublisher`. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-19 | Qoder | Drive Phase 2: added `IDriveNotificationPublisher` — Drive events notify explicit users (approvers, requesters, grantees) rather than confidentiality-filtered document audiences. |
| 2026-11-11 | Claude | Added `PublishToDepartmentAsync` so a decision can be announced to the document's owning department. |
| 2026-11-03 | Claude | Added the Module 10 contracts. |
