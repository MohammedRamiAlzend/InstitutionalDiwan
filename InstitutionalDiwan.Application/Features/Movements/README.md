# Application / Features / Movements — document routing between departments

> **Status:** Implemented — the full routing workflow (open, send, receive, complete, reject,
> return, redirect, close, cancel), the mandatory Central Diwan hub, rejection routing back to the
> Central Diwan, and the department inbox and outbox are live.

## Purpose

A document moves between departments through `DocumentMovement`. This slice turns that into use
cases: the transitions that move a document from one department to the next, and the two queues
every department works from — the **inbox** (documents waiting for the department) and the
**outbox** (documents the department sent and that are still elsewhere).

Receiving is the one transition that interacts with numbering: a department can only take custody
of a document after it has written its own register number, so `ReceiveDocument` looks up the
document's department register entry, refuses the receipt with
`Numbering.DepartmentRegistrationRequired` when it is missing, and otherwise records the step
together with the `DocumentNumber` id it was dated by.

## Contents

| File | Purpose |
| --- | --- |
| `CreateMovementCommand.cs` | CQRS command (document id, actor, destination department, type, assignee, instructions, due date) plus `MovementResponse`. |
| `CreateMovementCommandHandler.cs` | Requires a REGISTERED document, an active destination department, no other active movement, and a legal Central Diwan route; the source department is the caller's own primary department, never a client-supplied value. |
| `CreateMovementCommandValidator.cs` | FluentValidation rules: document id, actor, and destination department required. |
| `CentralDiwanRouting.cs` | Enforces the hub rule: a hop between two ordinary departments is refused unless the Central Diwan is the source or the destination. Same-department hand-offs are allowed. |
| `SendMovementCommand.cs` / `…Handler.cs` | Sends the current step to the destination department after re-checking the Central Diwan route. |
| `MovementStepGuard.cs` | Optional current-step check used by the planned `steps/{stepId}/…` route aliases; a stale step answers `Movements.StepNotCurrent`. |
| `ReceiveDocumentCommand.cs` / `…Handler.cs` / `…Validator.cs` | Records the receipt at the destination department, gated on that department's own register entry. |
| `CompleteMovementStepCommand.cs` / `…Handler.cs` | Completes the current step. |
| `RejectMovementStepCommand.cs` / `…Handler.cs` | Rejects the current step with a required reason and opens a new sent step back to the Central Diwan for administrative resolution (unless the Central Diwan itself rejected). |
| `ReturnMovementToSenderCommand.cs` / `…Handler.cs` | Returns the document to the sending department with a required reason. |
| `RedirectMovementCommand.cs` / `…Handler.cs` / `…Validator.cs` | Appends a step to a new destination without closing the movement. |
| `CloseMovementCommand.cs` / `…Handler.cs` | Closes the movement and releases the document. |
| `CancelMovementCommand.cs` / `…Handler.cs` | Cancels the movement with a required reason and releases the document. |
| `GetMovementByIdQuery.cs` / `…Handler.cs` | One movement with its steps and audit trail. |
| `ListDocumentMovementsQuery.cs` / `…Handler.cs` | Every movement of a document, newest first. |
| `GetDepartmentInboxQuery.cs` / `…Handler.cs` | The documents waiting in a department's inbox. |
| `GetDepartmentOutboxQuery.cs` / `…Handler.cs` | The documents a department sent out and that are still elsewhere. |
| `MovementQueue.cs` | Shared plumbing: resolves the department a queue query targets and turns movements into document-aware rows with a single batch lookup. |
| `MovementQueueItemResponse.cs` | One inbox/outbox row — the document's title and official number next to the movement and the current step. |
| `MovementResponse.cs` | A movement with its steps and events, used by the transition commands and the movement queries. |

## Conventions / Notes

- **The source department is never client-supplied.** `CreateMovementCommand` takes the actor and
  the destination; the handler reads the source from `user.PrimaryDepartmentId`.
- **One active movement per document.** A document that is already routed answers
  `Movements.ActiveMovementExists` (409) instead of opening a second movement.
- **The Central Diwan is the mandatory hub.** A transfer between two distinct ordinary departments
  answers `Movements.CentralDiwanRoutingRequired` (409); it must be split into ordinary → Central
  Diwan and Central Diwan → ordinary. Both sides are validated on open, send, and redirect, so a
  setting change cannot leave an illegal draft sendable. A missing hub answers
  `Movements.CentralDiwanNotConfigured` (409).
- **A rejection goes back to the hub.** The rejected step stays immutable and a new step from the
  rejecting department to the configured Central Diwan is appended and sent. When the Central Diwan
  is the rejecting department there is no self-step; the movement stays `REJECTED` and the sender's
  inbox receives it.
- **The planned step routes are aliases.** `/api/movements/{movementId}/steps/{stepId}/…` validates
  that the named step is current and then runs the same command as the movement-scoped route.
- **Only a REGISTERED document can be routed.** A draft answers
  `Correspondence.OnlyRegisteredRoutable` (409).
- The department number is **not** created by receiving. It must already exist, recorded through
  `POST /api/documents/{documentId}/numbers/department-registration`; recording and receiving in one
  request would defeat the "register before custody" rule.
- `DocumentMovement.ReceiveStep` itself rejects an empty register number, so the invariant holds even
  for a caller that bypasses the handler.
- **Queue membership follows the current step**, not the movement header:
  - *Inbox* — the current step is addressed to the department and has not left it yet (sent,
    received, or completed), so the document still awaits the department's action; plus the
    documents that came back because the receiving department rejected or returned them.
  - *Outbox* — the current step leaves the department and is sent, received, or completed, so the
    document is still with the receiving side.
  - Closed and canceled movements appear in neither queue. A redirect moves the custody: the new
    destination gets the document in its inbox and the department that redirected gets it in its
    outbox.
- **The department defaults to the caller's own.** `GetDepartmentInboxQuery` / `GetDepartmentOutboxQuery`
  take the caller's id and an optional department; an explicit department must be inside the
  caller's organizational scope, otherwise the answer is `Movements.DepartmentOutOfScope` (403).
- Transitions are exposed on `MovementsController` behind `DOCUMENT_EDIT`; the read endpoints
  (get, list, inbox, outbox) behind `DOCUMENT_VIEW`.

## Implementation status

| Item | State |
| --- | --- |
| `CreateMovementCommand` (+ handler, validator) | ✅ |
| `SendMovementCommand`, `ReceiveDocumentCommand`, `CompleteMovementStepCommand`, `RejectMovementStepCommand`, `ReturnMovementToSenderCommand`, `RedirectMovementCommand`, `CloseMovementCommand`, `CancelMovementCommand` | ✅ |
| `GetMovementByIdQuery`, `ListDocumentMovementsQuery` | ✅ |
| `GetDepartmentInboxQuery`, `GetDepartmentOutboxQuery` | ✅ |
| Mandatory Central Diwan hub + rejection routing | ✅ |
| Tracking chain across a document's movements | ✅ Covered by `ListDocumentMovementsQuery` |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Closed the Module 05 gaps: added `CentralDiwanRouting` to open/send/redirect, routed rejections back to the Central Diwan as a new sent step, added `MovementStepGuard` for current-step validation, and added the planned step/department/document route aliases. |
| 2026-10-22 | Buffy | The routing handlers now pass `IUnitOfWork.UtcNow` into every transition (`Create`, `Send`, `ReceiveStep`, `CompleteStep`, `RejectStep`, `ReturnToSender`, `RedirectTo`, `Close`, `Cancel`) and into `Document.MarkInMovement`/`MarkMovementEnded`, so steps and events in one transaction share the same instant. |
| 2026-10-21 | opencode | Implemented the whole routing workflow and the read side: added the open/send/complete/reject/return/redirect/close/cancel commands, the movement queries, and the department inbox and outbox (`GetDepartmentInboxQuery`, `GetDepartmentOutboxQuery`, `MovementQueue`, `MovementQueueItemResponse`). |
| 2026-10-20 | Buffy | Created the slice with `ReceiveDocumentCommand`: receiving is blocked until the destination department recorded its own register number, which is then stored on the movement step. |
