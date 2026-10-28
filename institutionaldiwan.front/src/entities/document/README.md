# institutionaldiwan.front / src/entities/document — Correspondence Document Entity Slice

> **Status:** Implemented — read models, query keys, hooks, badges, and the official-number tag.
> Write flows live in `features/document-*`; attachment versions stay in `entities/document-file`.

## Purpose

Represents the Module 03 Correspondence `Document` and its per-document `DocumentAccessGrant` read
models on the client. Owns `GET /api/documents`, `GET /api/documents/{documentId}`, details/timeline,
and `GET /api/documents/{documentId}/access-grants` queries, query-key factories, and read-only
status/direction/official-number UI. File versions belong in sibling `entities/document-file`
(Module 06) — do not fold attachment GETs into this slice.

`GET /api/documents` is server-paged (`PagedResult<T>` from `@/shared/types`); `useDocuments` takes
paging/search/sort/filter params and keeps the previous page visible while the next loads.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `DocumentResponse`, `DocumentParticipantResponse`, `DocumentStatusHistoryResponse`, `DocumentDetailResponse`, `DocumentTimelineEntryResponse`, `DocumentAccessGrantResponse`, and `DocumentListParams` matching `InstitutionalDiwan.Application`. |
| `model/queries.ts` | `documentKeys` factory plus `useDocuments`, `useDocumentById`, `useDocumentDetails`, `useDocumentTimeline`, and `useDocumentAccessGrants`. |
| `api/documentApi.ts` | Typed `GET` calls through `httpClient` from `@/shared/api`; blank search and department filters are dropped. |
| `ui/DocumentStatusBadge.tsx` | Arabic badges for `Draft`, `Registered`, `InMovement`, `Archived`, `Canceled` with a `#id` fallback. |
| `ui/DocumentDirectionBadge.tsx` | Arabic badges for `Internal`, `Incoming`, `Outgoing` with direction icons. |
| `ui/OfficialNumberTag.tsx` | Renders `officialNumberText` (and an optional numbering year) in `dir="ltr"` monospace, or «بدون رقم». |
| `index.ts` | Public barrel exporting types, query keys, hooks, and read-only components. |

## Query keys

`documentKeys.all` → `lists()` → `list(params)`; `details()` → `detail(id)` → `full(id)` /
`timeline(id)` / `grants(id)`. Features invalidate `documentKeys.lists()` after writes and
`documentKeys.detail(id)` after per-document changes.

## Permissions

- `useDocuments`, `useDocumentById`, `useDocumentDetails`, `useDocumentTimeline` — `DOCUMENT_VIEW`.
- `useDocumentAccessGrants` — `ACCESS_GRANT_MANAGE` (the slice does not gate the call itself; the
  grants tab enables it only for that permission).
- `DocumentListParams.owningDepartmentId` is the department register view; the server enforces the
  caller's organizational scope.

## Implementation status

| Item | State |
| --- | --- |
| `model/`, `api/`, `ui/`, `index.ts` | ✅ Implemented |
| `DocumentStatusBadge` labels | Hard-coded Arabic matching the seeded `document-status-types` catalog; no lookup round-trip. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | Qoder | Module 03 Phase 1: implemented types, API reads, query keys/hooks (list, by-id, details, timeline, grants), status/direction badges, and the official-number tag. |
| 2026-10-25 | Cursor | Clarified Module 06 files live in `entities/document-file`; pointed paging type at `@/shared/types`. |
| 2026-10-22 | opencode | Recorded that `GET /api/documents` is now server-paged (`PagedResult<DocumentResponse>`) and that `useDocuments` takes the paging/search/sort/filter input. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/document` slice folder and README. |
