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
| `ui/ReceiveMovementDialog.tsx` | Receives a movement with its register date, showing the department's number locked or asking for one when the year has none. |
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

- 2026-11-05: Unsent form input is now kept (per user, per form) after an accidental close or cancel and restored with a toast; it is cleared only after a successful submit (`useDraftForm`).
| Date | Author | Change |
| --- | --- | --- |
| 2026-11-10 | Claude | The receive dialog offers "تسجيل الوثيقة برقم جديد" when the department already has a number that year and sends `issueNewNumber`; the toast names the replaced number. |
| 2026-11-10 | Claude | The receive dialog shows the destination department's record form and sends `fieldValues`; movement writes also refresh department records. |
| 2026-11-08 | Claude | Receive asks for the register first when a department has several and a new number is needed. |
| 2026-11-08 | Claude | Receive has its own dialog with a date and a locked or typed department number. |
| 2026-10-29 | Cursor | Implemented the feature: mutation hooks, Zod schemas, action dialogs, and the inline action cluster; moved exported constants out of the component for `react-refresh`. |
| 2026-10-25 | Cursor | Added movement-routing feature docs for live Module 05 workflow commands. |
