# `src/entities/movement` — Document Movement Read Model (Module 05)

> **Status:** Docs only — API live; code pending.

## Purpose

Inbox/outbox queues, per-document movement lists, and step/status badges. Routing mutations live in
`features/movement-routing`.

## Planned Contents

| File | Purpose |
| --- | --- |
| `api/listInbox.ts` / `listOutbox.ts` | `GET /api/movements/inbox` / `outbox` |
| `api/listByDocument.ts` | `GET /api/movements/by-document/{documentId}` |
| `api/getMovement.ts` | `GET /api/movements/{movementId}` |
| `model/types.ts` | Movement / step / event response types |
| `model/queries.ts` | `movementKeys` + hooks |
| `model/toMermaid.ts` | Pure builder that renders a document's movement step chain as Mermaid `flowchart TD` for `shared/ui/mermaid-view`. |
| `model/toTree.ts` | Pure builder that renders the step chain as a typed tree for `widgets/visualization-panel`. |
| `ui/MovementStepBadge.tsx` | Step status presentation |
| `index.ts` | Public barrel |

## Query keys (contract)

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

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Added pure `toMermaid` / `toTree` builders for the document movement chain. |
| 2026-10-25 | Cursor | Added movement entity slice docs mapped to live Module 05 inbox/outbox APIs. |
