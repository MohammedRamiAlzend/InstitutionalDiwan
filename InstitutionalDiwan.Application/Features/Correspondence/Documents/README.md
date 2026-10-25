# Application / Features / Correspondence / Documents

> **Status:** Implemented — full Module 03 lifecycle: immediate create, draft create, metadata
> update, register, participants, cancel, detail, timeline, a paged, searchable, filterable list, and
> the same list narrowed to one department's register.

## Purpose

Self-contained CQRS slice for document lifecycle. Two write paths exist: `CreateDocumentCommand`
(legacy single-step path that takes an explicit official number, validates the number+year pair,
issues a `DocumentNumber`, and saves the document as `REGISTERED`) and the explicit lifecycle
commands the API actually walks: `CreateDraftDocumentCommand` → `UpdateDraftMetadataCommand` →
`RegisterDocumentCommand` (allocates the number from the matching registry) →
`AddDocumentParticipantCommand` / `CancelDocumentCommand`, with `GetDocumentDetailsQuery` and
`GetDocumentTimelineQuery` for reads.

## Contents

| File | Purpose |
| --- | --- |
| `CreateDocumentCommand.cs` | CQRS command carrying `UserId` (from the token), document metadata, plus `OfficialNumberText` and `NumberingYear`; defines the create contract. |
| `CreateDocumentCommandHandler.cs` | Orchestrates user validation → duplicate-number check → `Document.CreateDraft` → registry resolution → `DocumentNumber.Issue` → `Document.Register` → save; the document ends up `REGISTERED`. |
| `CreateDocumentCommandValidator.cs` | FluentValidation rules: required title/type/direction, direction ∈ {INTERNAL, INCOMING, OUTGOING}, required official number (≤100 chars), numbering year in [1900, 2200]. |
| `CreateDraftDocumentCommand.cs` | CQRS command creating a DRAFT document owned by the caller's department, without consuming an official number. |
| `CreateDraftDocumentCommandHandler.cs` | Validates the caller (active user) and document type (exists + active), then `Document.CreateDraft` with the caller's `PrimaryDepartmentId` as owner. |
| `CreateDraftDocumentCommandValidator.cs` | FluentValidation rules for title, type, direction, and optional summary/date/reference fields. |
| `UpdateDraftMetadataCommand.cs` | CQRS command editing title, summary, confidentiality, and priority. |
| `UpdateDraftMetadataCommandHandler.cs` | Loads the document (404) and applies `Document.UpdateDraftMetadata`, which refuses anything but `DRAFT` (`Correspondence.OnlyDraftEditable`, 409). |
| `UpdateDraftMetadataCommandValidator.cs` | FluentValidation rules for title/summary and positive optional level ids. |
| `RegisterDocumentCommand.cs` | CQRS command transitioning DRAFT → REGISTERED by allocating an official number (optional explicit year). |
| `RegisterDocumentCommandHandler.cs` | Validates draft status *before* allocating (a rejected call never burns a sequence), resolves the registry, allocates, renders via `OfficialNumberFormatter`, checks `(text, year)` uniqueness, then `DocumentNumber.Issue` + `Document.Register` + save. |
| `AddDocumentParticipantCommand.cs` | CQRS command attaching an external organization, internal department, or person to a document. |
| `AddDocumentParticipantCommandHandler.cs` | Enforces exactly-one target, validates the participant role lookup and the referenced department/organization (exists + active), then `Document.AddParticipant`, which enforces the single-external-party limit. |
| `AddDocumentParticipantCommandValidator.cs` | FluentValidation rules incl. the exactly-one-target invariant (`Correspondence.ParticipantTargetInvalid`). |
| `CancelDocumentCommand.cs` | CQRS command cancelling a document with a required reason. |
| `CancelDocumentCommandHandler.cs` | Asks `IDocumentMovementRepository.HasActiveMovementsAsync` first, then `Document.Cancel` (blocked by active movements, archived status, or a missing reason). |
| `CancelDocumentCommandValidator.cs` | FluentValidation rules: ids required, non-blank reason ≤ 1000 chars. |
| `DocumentResponse.cs` | Light read model returned by list/create/register/cancel; `From(Document)` maps the aggregate incl. `OfficialNumberText` and `RegistrationDate`. |
| `DocumentDetailResponse.cs` | Rich read model: the document plus its participants and status history (ordered). |
| `DocumentParticipantResponse.cs` | Read model for one participant row (target type, ids/person, primary flag, notes). |
| `DocumentStatusHistoryResponse.cs` | Read model for one recorded status transition. |
| `DocumentTimelineEntryResponse.cs` | Read model for one audit-timeline entry (`EventKind` ∈ `STATUS` / `PARTICIPANT` / `NUMBER`). |
| `GetDocumentByIdQuery.cs` / `GetDocumentByIdQueryHandler.cs` | Fetches one document by id (NotFound when missing). |
| `GetDocumentDetailsQuery.cs` / `GetDocumentDetailsQueryHandler.cs` | Fetches the document with participants and status history. |
| `GetDocumentTimelineQuery.cs` / `GetDocumentTimelineQueryHandler.cs` | Merges status transitions, participant additions, and official-number events into one oldest-first timeline. |
| `ListDocumentsQuery.cs` / `ListDocumentsQueryHandler.cs` | Returns a page of documents (`PagedResult<DocumentResponse>`), newest first by default. Carries `Page`/`PageSize`/`Search`/`SortBy`/`SortDescending` plus `StatusId`/`OwningDepartmentId`/`DirectionId`/`DocumentTypeId` filters; the handler builds a `PageRequest` and delegates to `IDocumentRepository.ListPagedAsync`, so filtering, sorting, and `Skip`/`Take` run in SQL. |
| `ListDepartmentDocumentsQuery.cs` / `ListDepartmentDocumentsQueryHandler.cs` | One department's document register: the paged/searchable/sortable contract of `ListDocumentsQuery` with the department as the filter. The handler returns 404 for an unknown department and 403 (`Correspondence.DepartmentOutOfScope`) when the department sits outside the caller's scope, then delegates to `IDocumentRepository.ListPagedAsync`. |
| `README.md` | This file. |

## Conventions / Notes

- `UserId` always comes from the JWT via the controller — never from the request body.
- Duplicate official numbers are rejected with `DomainErrors.Numbering.OfficialNumberTaken`
  (`ErrorType.Conflict` → 409); the same text in a **different** year is allowed.
- Registration validates the document state *before* allocating a sequence so a 409 never leaves a
  gap in the registry counter.
- The number's internal format/order is not validated against the registry's `FormatPattern` —
  uniqueness is purely the (text, year) pair, backed by the `UX_DOC_NUMBERS_REG_YEAR_SEQ` index.
- Reads (`GET`, `POST /drafts` etc.) are guarded by `DOCUMENT_VIEW` / `DOCUMENT_CREATE` /
  `DOCUMENT_EDIT`; timing-safe access checks per document (grants, confidentiality) live in
  `Infrastructure/Authorization/DocumentAccessEvaluator`.
- `ListDocumentsQuery` sorts by `createdAt` (default, newest first), `title`, `registrationDate`, or
  `officialNumber`; an unknown `sortBy` falls back to `createdAt`. `search` matches the title, the
  subject summary, and the official number.
- `pageSize` is clamped to 1–100 and `page` to ≥1, so a hostile query string cannot read the table in
  one request. The response always carries `totalCount`, `totalPages`, `hasPrevious`, and `hasNext`.
- `ListDepartmentDocumentsQuery` is the register view behind `GET /api/departments/{departmentId}/documents`.
  It checks the department through `IDepartmentScopeEvaluator` (tier 2 of the access model) before
  reading, so an empty page never masks a department the caller may not see.
- Every handler stamps the aggregate with `IUnitOfWork.UtcNow`; the domain methods take it as an
  optional `utcNow` argument so tests can still construct documents at a fixed instant.

## Implementation status

| Item | State |
| --- | --- |
| `CreateDocumentCommand` (+ handler/validator) with official-number conflict check | ✅ |
| `CreateDraftDocumentCommand`, `UpdateDraftMetadataCommand`, `RegisterDocumentCommand` | ✅ |
| `AddDocumentParticipantCommand`, `CancelDocumentCommand` | ✅ |
| `GetDocumentByIdQuery`, `GetDocumentDetailsQuery`, `GetDocumentTimelineQuery` | ✅ |
| `ListDocumentsQuery` (paged, searchable, sortable, filtered) | ✅ |
| `ListDepartmentDocumentsQuery` (paged + scope-checked) | ✅ |
| Archive/unarchive use cases (Module 07) | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | Buffy | Added `ListDepartmentDocumentsQuery`/`Handler` — a department's document register, paged like `ListDocumentsQuery`, authorized with `IDepartmentScopeEvaluator` (403 `Correspondence.DepartmentOutOfScope`, 404 for an unknown department). Handlers now stamp document writes with `IUnitOfWork.UtcNow`. |
| 2026-10-22 | opencode | `ListDocumentsQuery` now returns `PagedResult<DocumentResponse>` and accepts `page`, `pageSize`, `search`, `sortBy`, `sortDescending`, `statusId`, `owningDepartmentId`, `directionId`, and `documentTypeId`; `DocumentRepository.ListPagedAsync` pushes the filter, sort, and paging into SQL. |
| 2026-10-19 | Buffy | Completed the Module 03 lifecycle: added draft create, draft metadata update, register (registry-backed allocation), add participant (exactly-one-target + single external party), cancel (blocked by active movements), document details, and the merged audit timeline; added the corresponding response records. |
| 2026-10-18 | opencode | Added `OfficialNumberText` + `NumberingYear` to create; handler now performs the taken/not-taken check (409 on conflict), issues and registers an official number at creation, and `DocumentResponse` exposes the number + registration date. |
