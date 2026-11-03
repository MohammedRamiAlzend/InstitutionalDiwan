# `src/entities/archive` — Archive Reads

> **Status:** Implemented — archive search, archive history and saved-search reads.

## Purpose

Read side of Module 07: the access-filtered archive search, a document's archive history, and the
caller's saved searches. Writes live in `features/archive-manage`.

## Contents

| File | Purpose |
| --- | --- |
| `api/archiveApi.ts` | `search`, `events`, `savedSearches` → `GET /api/archive/documents`, `/api/documents/{id}/archive-events`, `/api/saved-searches`. |
| `model/types.ts` | `ArchivedDocumentResponse`, `ArchiveSearchParams`, `DocumentArchiveEventResponse`, `SavedSearchResponse`. |
| `model/queries.ts` | `archiveKeys` factory and the `useArchivedDocuments`, `useDocumentArchiveEvents`, `useSavedSearches` hooks. |
| `model/criteria.ts` | Serialises and defensively parses saved-search criteria (the archive grid's URL params). |
| `ui/ArchiveEventTypeBadge.tsx` | Archived / unarchived / retention-applied badge with its Arabic label. |
| `index.ts` | Public barrel. |

## Notes

- The search counters follow the same visibility rule as the rows, so `totalCount` never leaks hidden documents.
- Retention periods are out of scope for the API, so `RETENTION_APPLIED` events do not occur yet.

## Implementation status

| Item | State |
| --- | --- |
| Reads, keys, hooks, badge, criteria helpers | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | agent | Implemented the Module 07 read slice. |
