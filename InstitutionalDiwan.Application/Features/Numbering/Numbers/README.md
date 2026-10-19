# Application / Features / Numbering / Numbers — official-number lifecycle

> **Status:** Implemented — issue, cancel, replace, and full history.

## Purpose

The four use cases around a document's official number: allocate the next gap-free sequence from the
matching registry and register the draft, cancel a number with a documented reason (the sequence is
never reused), replace a number with a freshly allocated one while linking the old row to its
replacement, and list every number ever issued for a document with its event history.

## Contents

| File | Purpose |
| --- | --- |
| `DocumentNumberResponse.cs` | Read model for one official number incl. its ordered event history; `DocumentNumberHistoryResponse` is the nested entry record. |
| `IssueOfficialNumberCommand.cs` | CQRS command (document id, actor, optional numbering year) that issues the next number. |
| `IssueOfficialNumberCommandHandler.cs` | Validates draft status + no active number *before* allocating, resolves the registry, allocates, renders, checks uniqueness, issues the number, registers the document, and saves both aggregates. |
| `CancelOfficialNumberCommand.cs` / `CancelOfficialNumberCommandHandler.cs` | Cancels an active number with a required reason (404 for a mismatched document/number, 409 when not active) via `DocumentNumber.Cancel`. |
| `CancelOfficialNumberCommandValidator.cs` | FluentValidation rules for the cancel payload (ids required, non-blank reason ≤ 1000 chars). |
| `ReplaceOfficialNumberCommand.cs` | CQRS command issuing a replacement and linking it to the number being corrected. |
| `ReplaceOfficialNumberCommandHandler.cs` | Allocates the replacement, marks the old number `REPLACED` with `ReplacedByNumberId`, re-points the document via `Document.AttachReplacementNumber`, and saves. |
| `ReplaceOfficialNumberCommandValidator.cs` | FluentValidation rules for the replace payload. |
| `GetDocumentNumberHistoryQuery.cs` / `GetDocumentNumberHistoryQueryHandler.cs` | Lists every number of a document oldest-first (404 when the document does not exist). |

## Conventions / Notes

- `UserId` comes from the token via the controller; it identifies who issued/cancelled/replaced the
  number and is recorded on `DocumentNumber` and its history rows.
- Number status is asserted explicitly (`NumberNotActive`, 409) before the domain call so the error
  code is stable regardless of which domain method runs first.

## Implementation status

| Item | State |
| --- | --- |
| `IssueOfficialNumberCommand` (+ handler) | ✅ |
| `CancelOfficialNumberCommand` (+ handler, validator) | ✅ |
| `ReplaceOfficialNumberCommand` (+ handler, validator) | ✅ |
| `GetDocumentNumberHistoryQuery` (+ handler) | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-19 | Buffy | Added the four official-number use cases, backed by `NumberingRegistry.AllocateNextSequence`, `DocumentNumber.Issue/Cancel/ReplaceWith`, and the new `Document.AttachReplacementNumber`; endpoints exposed on `DocumentNumbersController` behind `NUMBERING_MANAGE` / `NUMBERING_VIEW`. |
