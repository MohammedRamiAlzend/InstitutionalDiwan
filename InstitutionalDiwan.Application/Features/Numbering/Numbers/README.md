# Application / Features / Numbering / Numbers — official-number lifecycle

> **Status:** Implemented — issue, cancel, replace, full history, department register entries.

## Purpose

The use cases around a document's numbers. The central Diwan number allocates the next gap-free
sequence from the matching registry and registers the draft; it can be cancelled with a documented
reason (the sequence is never reused) or replaced with a freshly allocated number linked to its
predecessor, and every number ever issued for a document is listed with its event history.

Alongside it, `RecordDepartmentRegistrationCommand` writes what a department's clerk typed in that
department's own register. Nothing is allocated there: the clerk supplies the number and the date, the
central number is untouched, and the same department registering the document again keeps the number
and only appends a new dated history entry. `GetDocumentRegistryNumbersQuery` returns both worlds for
one document.

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
| `RecordDepartmentRegistrationCommand.cs` | CQRS command carrying document id, department, the typed register number, the register date, and the actor. |
| `RecordDepartmentRegistrationCommandHandler.cs` | Requires the department's own active registry (never the shared default), rejects a draft without a central number, keeps one line per department, and either creates the entry or re-dates the existing one. |
| `RecordDepartmentRegistrationCommandValidator.cs` | FluentValidation rules: ids required, non-blank number ≤ 100 chars, plausible register date, notes ≤ 1000 chars. |
| `DocumentRegistryNumbersResponse.cs` | Read model pairing the central number with per-department register lines and their dated registration events. |
| `GetDocumentRegistryNumbersQuery.cs` / `GetDocumentRegistryNumbersQueryHandler.cs` | Returns the central number plus every department register line for a document (404 when missing). |

## Conventions / Notes

- `UserId` comes from the token via the controller; it identifies who issued/cancelled/replaced the
  number and is recorded on `DocumentNumber` and its history rows.
- Number status is asserted explicitly (`NumberNotActive`, 409) before the domain call so the error
  code is stable regardless of which domain method runs first.
- Central and department numbers share `DocumentNumber`; `ScopeId` separates them. Cancel/replace
  lookups are scoped to `ScopeId = Central` so department lines are never mistaken for the official
  number, and `DocumentNumber.ReplaceWith` refuses a department entry outright.
- A department entry is written **only** through `Document.AddDepartmentNumber`, which enforces one
  line per department; the idempotent read (`FindDepartmentNumber`) is what turns a second arrival
  into a new date instead of a second row.

## Implementation status

| Item | State |
| --- | --- |
| `IssueOfficialNumberCommand` (+ handler) | ✅ |
| `CancelOfficialNumberCommand` (+ handler, validator) | ✅ |
| `ReplaceOfficialNumberCommand` (+ handler, validator) | ✅ |
| `GetDocumentNumberHistoryQuery` (+ handler) | ✅ |
| `RecordDepartmentRegistrationCommand` (+ handler, validator) | ✅ |
| `GetDocumentRegistryNumbersQuery` (+ handler) | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | Buffy | Issue/cancel/replace and department-registration handlers now pass `IUnitOfWork.UtcNow` into the registry allocation and the number factories, so the counter, the number, and its history share one instant. |
| 2026-10-20 | Buffy | Added department register entries: a clerk-typed number and date per department (`RecordDepartmentRegistrationCommand`), a second arrival that reuses the number and adds a dated history row, and `GetDocumentRegistryNumbersQuery` to read the central number alongside every department line. |
| 2026-10-19 | Buffy | Added the four official-number use cases, backed by `NumberingRegistry.AllocateNextSequence`, `DocumentNumber.Issue/Cancel/ReplaceWith`, and the new `Document.AttachReplacementNumber`; endpoints exposed on `DocumentNumbersController` behind `NUMBERING_MANAGE` / `NUMBERING_VIEW`. |
