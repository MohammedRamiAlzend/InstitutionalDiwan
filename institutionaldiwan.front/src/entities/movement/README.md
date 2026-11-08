# `src/entities/movement` — Document Movement Read Model (Module 03)

> **Status:** Implemented — read model, badges, and query hooks. Routing commands live in
> `features/movement-routing`.

## Purpose

Inbox/outbox queues, per-document movement lists with steps and events, plus movement/step/type
badges. Everything here is read-only.

## Contents

| File | Purpose |
| --- | --- |
| `api/movementApi.ts` | `GET /api/movements/inbox`, `/outbox`, `/by-document/{documentId}`, `/{movementId}` |
| `model/types.ts` | Movement, step, event, queue item and receipt response types; `MovementAction` union |
| `model/queries.ts` | `movementKeys` + `useMovementQueue`, `useMovementInbox`, `useMovementOutbox`, `useDocumentMovements`, `useMovementById` |
| `model/rules.ts` | `isMovementTerminal`, `hasActiveMovement`, `currentMovementStep`, `getMovementCustody`, `getMovementActions`, `translateMovementNote` |
| `ui/MovementStatusBadge.tsx` | Movement status badge from the `movement-statuses` lookup |
| `ui/MovementStepStatusBadge.tsx` | Step status badge from the `movement-step-statuses` lookup |
| `ui/MovementTypeBadge.tsx` | Movement type badge from the `movement-types` lookup |
| `index.ts` | Public barrel |

Deferred: `toMermaid` / `toTree` builders belong to `widgets/visualization-panel` and
`shared/ui/mermaid-view` in Module 05; the movement chain is currently rendered as an ordered
timeline in `pages/documents`.

## Query keys

```ts
export const movementKeys = {
  all: ['movements'] as const,
  inbox: (departmentId?: string) =>
    [...movementKeys.all, 'inbox', departmentId ?? 'me'] as const,
  outbox: (departmentId?: string) =>
    [...movementKeys.all, 'outbox', departmentId ?? 'me'] as const,
  byDocument: (documentId: string) =>
    [...movementKeys.all, 'by-document', documentId] as const,
  detail: (movementId: string) => [...movementKeys.all, 'detail', movementId] as const,
};
```

Queues keep `staleTime` at 15 seconds; every routing mutation invalidates the whole
`movementKeys.all` prefix so inbox, outbox, and the document timeline refresh together.

## Rules mirrored from the API

`getMovementActions(statusId, stepStatusId, canEdit, custody)` mirrors the domain transitions and the
API's custody checks so the UI only offers valid actions; the API remains the final judge
(`Movements.InvalidStatusTransition`, `Movements.DepartmentOutOfScope`). `getMovementCustody(step,
coversDepartment)` builds `custody`; the caller passes `useCoversDepartment` from `entities/user`,
because this slice may not import a sibling.

| Status | Actions |
| --- | --- |
| Draft | send, redirect, cancel |
| Sent | receive (when the step is sent), reject, redirect, close, cancel |
| Received / InProgress | complete, reject, return, redirect, close, cancel |
| Redirected | send, close, cancel |
| Completed / Rejected / Returned | close |
| Closed / Canceled | — |

The status list is then narrowed to the caller's side of the current step:

| Side | Actions |
| --- | --- |
| Sender | send, cancel, and redirect while the movement is a draft |
| Receiver | receive, complete, reject, return, and redirect once sent |
| Either | close |

`translateMovementNote` rewrites the API's English system notes (`Rejected: …`,
`Rejected step routed to the Central Diwan: …`) into Arabic before rendering.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ✅ |
| Browser-verified | ✅ (inbox/outbox, document timeline, reroute after rejection, actions per side of the step) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-08 | Claude | Steps carry the register number and visit date; the receipt reports `registryVisitId` and `isReturnVisit`. |
| 2026-11-03 | Claude | `getMovementActions` now takes the caller's custody of the current step, built by the new `getMovementCustody`, and mirrors the API's sender and receiver checks. |
| 2026-10-29 | Cursor | Implemented the read model: API client, query keys/hooks, rules, and three badges; documented the deferred Mermaid/tree builders. |
| 2026-10-25 | Buffy | Added pure `toMermaid` / `toTree` builders for the document movement chain. |
| 2026-10-25 | Cursor | Added movement entity slice docs mapped to live Module 05 inbox/outbox APIs. |
