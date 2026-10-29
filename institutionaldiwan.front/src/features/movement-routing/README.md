# `src/features/movement-routing` — Document Routing Actions (Module 03)

> **Status:** Implemented and browser-verified.

## Purpose

Every routing command for a document movement: open, send, receive, complete, reject, return,
redirect, close, cancel. Successful mutations invalidate the whole `movementKeys.all` prefix so
inbox, outbox, and the document timeline refresh together.

## Contents

| File | Purpose |
| --- | --- |
| `api/movementMutations.ts` | One mutation per route under `/api/movements`; invalidates `movementKeys` |
| `model/types.ts` | Request payload types |
| `model/schemas.ts` | Zod schemas (`movementNotesSchema`, `movementReasonSchema`, `redirectMovementSchema`, `openMovementSchema`) |
| `model/constants.ts` | Arabic action labels, action icons, destructive-action set |
| `model/useAssigneeOptions.ts` | Assignee search options for redirect/open |
| `ui/OpenMovementDialog.tsx` | Opens a movement from a registered document |
| `ui/MovementActionDialog.tsx` | One dialog per action (optional notes, required reason, redirect form) |
| `ui/MovementActionButtons.tsx` | Inline action cluster filtered by `getMovementActions` |
| `index.ts` | Public barrel |

## Domain rules encoded in the UI

- Opening a movement is only offered for `REGISTERED` documents without an active movement; the
  destination list includes the Central Diwan requirement, and `Movements.CentralDiwanRoutingRequired`
  / `Movements.CentralDiwanNotConfigured` surface as Arabic banners.
- The receive dialog explains that the destination department must have recorded its own register
  number; when it has not, the API answers `Numbering.DepartmentRegistrationRequired` and the
  mutation error is rendered inline.
- The reject dialog states that a non-central department's rejection routes the document back to the
  Central Diwan as a new step.
- Every write requires `DOCUMENT_EDIT`; the API remains the final judge and answers
  `Movements.InvalidStatusTransition` etc., rendered through `toArabicErrorMessage`.

## Implementation status

| Item | State |
| --- | --- |
| Docs | ✅ |
| Code | ✅ |
| Browser-verified | ✅ (open → send → receive → reject → receive at central → close) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | Cursor | Implemented the feature: mutation hooks, Zod schemas, action dialogs, and the inline action cluster; moved exported constants out of the component for `react-refresh`. |
| 2026-10-25 | Cursor | Added movement-routing feature docs for live Module 05 workflow commands. |
