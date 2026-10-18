# Application / Features / Correspondence / Documents

> **Status:** Implemented — create (with official number), fetch by id, and recent-list slices.

## Purpose

Self-contained CQRS slice for document lifecycle reads and creation. `CreateDocumentCommand` is the
single write use case here: it validates the caller, checks that the supplied official number +
numbering year pair is not already taken (Conflict → HTTP 409), creates the `Document` draft,
allocates the next sequence from the matching numbering registry (auto-creating a `DEFAULT` registry
when none exists), issues a `DocumentNumber`, registers the document, and persists both aggregates
in one `SaveChanges`.

## Contents

| File | Purpose |
| --- | --- |
| `CreateDocumentCommand.cs` | CQRS command carrying `UserId` (from the token), document metadata, plus `OfficialNumberText` and `NumberingYear`; defines the create contract. |
| `CreateDocumentCommandHandler.cs` | Orchestrates user validation → duplicate-number check → `Document.CreateDraft` → registry resolution → `DocumentNumber.Issue` → `Document.Register` → save; the document ends up `REGISTERED`. |
| `CreateDocumentCommandValidator.cs` | FluentValidation rules: required title/type/direction, direction ∈ {INTERNAL, INCOMING, OUTGOING}, required official number (≤100 chars), numbering year in [1900, 2200]. |
| `DocumentResponse.cs` | Read model returned by all three endpoints; `From(Document)` maps the aggregate incl. `OfficialNumberText` and `RegistrationDate`. |
| `GetDocumentByIdQuery.cs` / `GetDocumentByIdQueryHandler.cs` | Fetches one document by id (NotFound when missing). |
| `ListDocumentsQuery.cs` / `ListDocumentsQueryHandler.cs` | Returns the most recent documents, newest first. |

## Conventions / Notes

- `UserId` always comes from the JWT via the controller — never from the request body.
- Duplicate official numbers are rejected with `DomainErrors.Numbering.OfficialNumberTaken`
  (`ErrorType.Conflict` → 409); the same text in a **different** year is allowed.
- The number's internal format/order is not validated against the registry's `FormatPattern` —
  uniqueness is purely the (text, year) pair, backed by the `UX_DOC_NUMBERS_TEXT_YEAR` index.

## Implementation status

| Item | State |
| --- | --- |
| `CreateDocumentCommand` (+ handler/validator) with official-number conflict check | ✅ |
| `GetDocumentByIdQuery`, `ListDocumentsQuery` | ✅ |
| `UpdateDraftMetadataCommand`, `AddDocumentParticipantCommand`, `RegisterDocumentCommand`, `CancelDocumentCommand` | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-18 | opencode | Added `OfficialNumberText` + `NumberingYear` to create; handler now performs the taken/not-taken check (409 on conflict), issues and registers an official number at creation, and `DocumentResponse` exposes the number + registration date. |
