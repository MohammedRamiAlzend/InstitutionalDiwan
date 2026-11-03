# `src/pages/archive` — Archive (`/archive`)

> **Status:** Implemented — `/archive` with the archive search grid and saved searches, route and sidebar entry active.

## Purpose

Module 07 search surface: the archived documents the caller may view, filterable by location,
direction, type, owning department and archive date range, with personal saved searches. Archiving
and unarchiving happen on the document details page (`pages/documents`), not here.

## Routes & URL state

`/archive?page=&pageSize=&search=&sort=&dir=&archiveLocationId=&directionId=&documentTypeId=&owningDepartmentId=&archivedFrom=&archivedTo=`

`sort` is `archivedAt` (default, descending), `title` or `officialNumber`. The page is wrapped in
`RequirePermission` for `DOCUMENT_VIEW`; the server further limits rows to documents the caller can see.

## Contents

| File | Purpose |
| --- | --- |
| `ui/ArchivePage.tsx` | Composes the page header and `widgets/archive-search-table` inside a `DOCUMENT_VIEW` guard and the `archive` tutorial guide. |
| `index.ts` | Public barrel. |

## Permissions

- Page, search, saved searches: `DOCUMENT_VIEW`

## Implementation status

| Item | State |
| --- | --- |
| Slice docs | ✅ |
| Page code | ✅ (`npm run build` and `npm run lint` pass; verified against the live API) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | agent | Implemented the page, the `/archive` route and the «الأرشيف» sidebar entry for Module 07. |
