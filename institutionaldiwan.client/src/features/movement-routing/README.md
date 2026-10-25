# `src/features/movement-routing` — Document Routing Actions (Module 05)

> **Status:** Docs only — API live; code pending.

## Purpose

Mutations for open/send/receive/complete/reject/return/redirect/close/cancel. Invalidate
`movementKeys` (inbox/outbox/by-document/detail) on success.

## Planned Contents

| File | Purpose |
| --- | --- |
| `api/*.ts` | One function per action route under `/api/movements` |
| `model/*.schema.ts` | Zod schemas (reason required for reject/return; destination for redirect) |
| `ui/MovementActionsMenu.tsx` | Context menu of allowed actions for current step |
| `index.ts` | Public barrel |

## Domain rules to encode in UI

- Central Diwan hub routing where required by the API.
- Receive disabled until department registration number exists.
- Surface `Movements.InvalidStatusTransition`, `Movements.DepartmentOutOfScope`, etc. via `ApiError.code`.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ❌ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Added movement-routing feature docs for live Module 05 workflow commands. |
