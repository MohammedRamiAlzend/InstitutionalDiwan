# institutionaldiwan.client / src/entities/document — Correspondence Document Entity Slice
> **Status:** Scaffolding & Rules — Entity read models, query keys, and UI badges defined; code pending.
## Purpose
Represents the Module 03 Correspondence `Document` and its per-document `DocumentAccessGrant` read models on the client. Owns `GET /api/documents`, `GET /api/documents/{documentId}`, and `GET /api/documents/{documentId}/access-grants` queries, query-key factories, and read-only status/direction/confidentiality badges.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `DocumentResponse` and `DocumentAccessGrantResponse` TypeScript contracts matching `InstitutionalDiwan.Application`. |
| `model/queries.ts` | `documentKeys` factory and `useDocuments`, `useDocumentById`, `useDocumentAccessGrants` TanStack Query hooks. |
| `api/documentApi.ts` | Typed `GET` calls using `httpClient` from `@/shared/api`. |
| `ui/DocumentStatusBadge.tsx` | Renders `Draft`, `Registered`, `InMovement`, `Archived`, `Canceled` status badges. |
| `ui/DocumentDirectionBadge.tsx` | Renders `Incoming`, `Outgoing`, `Internal` direction badges. |
| `ui/OfficialNumberTag.tsx` | Renders `OfficialNumberText` + `NumberingYear` in `dir="ltr"` monospace formatting. |
| `index.ts` | Public barrel exporting types, query keys, hooks, and read-only badge components. |
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `api/`, `ui/`, `index.ts` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/document` slice folder and README. |
