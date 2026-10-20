# Application / Features / Movements — document routing between departments

> **Status:** Partially implemented — receiving a step (with the department register gate) is live;
> the rest of the movement workflow is still domain-only.

## Purpose

A document moves between departments through `DocumentMovement`. This slice turns that into use
cases. So far it covers the one transition that interacts with numbering: **receiving**. A department
can only take custody of a document after it has written its own register number, so `ReceiveDocument`
looks up the document's department register entry, refuses the receipt with
`Numbering.DepartmentRegistrationRequired` when it is missing, and otherwise records the step together
with the `DocumentNumber` id it was dated by.

The other transitions (`Create`, `Send`, `Complete`, `Reject`, `Return`, `Redirect`, `Close`, `Cancel`)
already exist on `DocumentMovement` and only need handlers + endpoints.

## Contents

| File | Purpose |
| --- | --- |
| `ReceiveDocumentCommand.cs` | CQRS command (movement id, actor, notes) plus `MovementStepReceiptResponse`, which reports the step, destination department, and the register number/date that gated it. |
| `ReceiveDocumentCommandHandler.cs` | Loads the movement and the document with its numbers, requires the destination department's register entry, calls `DocumentMovement.ReceiveStep(actor, registryNumberId)`, and saves. |
| `ReceiveDocumentCommandValidator.cs` | FluentValidation rules: movement id and actor required, notes ≤ 1000 chars. |

## Conventions / Notes

- The department number is **not** created here. It must already exist, recorded through
  `POST /api/documents/{documentId}/numbers/department-registration`; recording and receiving in one
  request would defeat the "register before custody" rule.
- Receipt is exposed on `MovementsController` behind `DOCUMENT_EDIT`.
- `DocumentMovement.ReceiveStep` itself rejects an empty register number, so the invariant holds even
  for a caller that bypasses this handler.

## Implementation status

| Item | State |
| --- | --- |
| `ReceiveDocumentCommand` (+ handler, validator) | ✅ |
| `SendDocumentCommand`, `RedirectDocumentCommand`, `CompleteStepCommand`, `RejectDocumentCommand`, `ReturnDocumentCommand`, `CloseMovementCommand` | ❌ Not implemented |
| Inbox/outbox/tracking queries | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-20 | Buffy | Created the slice with `ReceiveDocumentCommand`: receiving is blocked until the destination department recorded its own register number, which is then stored on the movement step. |
