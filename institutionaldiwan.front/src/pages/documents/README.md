# `src/pages/documents` — Correspondence Registry & Document Details (`/documents`)

> **Status:** Registry page ✅ implemented (Module 03 Phase 2) — `/documents` gates on `DOCUMENT_VIEW` and mounts `widgets/document-registry-table`. Document details (`/documents/:documentId`) remain planned for Phase 3.

## Purpose

Renders the correspondence registry at `/documents` and, from Phase 3, the document detail at
`/documents/:documentId`. Detail composes metadata, numbering, access grants, movement timeline, and
the **attachments panel** (Module 06).

## Strict FSD Rules

- Allowed: `@/widgets/*`, `@/features/*`, `@/entities/*`, `@/shared/*`.
- Forbidden: `@/app/*`, sibling pages.
- Public API: export `DocumentsPage` (and later `DocumentDetailsPage`) via `index.ts`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentsPage.tsx` | ✅ Registry page: `RequirePermission DOCUMENT_VIEW`, «سجل الوثائق» header, registry table. |
| `ui/DocumentDetailsPage.tsx` | ⏳ Phase 3 — tabs/sections: details, participants, numbers, movements, files, grants. |
| `index.ts` | Public barrel (currently `DocumentsPage`). |

## Widgets / features composed

- `widgets/document-registry-table` ✅
- `widgets/document-attachments-panel` ← `GET/POST /api/documents/{id}/files` (planned)
- `widgets/movement-timeline` (planned)
- `features/document-create` ✅, `document-access-grant`, `attachment-upload`, `movement-routing` (planned)

## URL state

- List: `?page=&pageSize=&search=&statusId=&directionId=&documentTypeId=&owningDepartmentId=&sortBy=&sortDescending=`
- Detail: `?tab=details|files|movements|grants` (Phase 3)

## Table contract

The registry mounts `widgets/document-registry-table`, which uses the shared data grid: URL-driven
filters/sort/paging, a column chooser persisted under the `documents` table id, permission-filtered
row and header context menus, and an always-visible row overflow trigger. Detail tabs that show
lists (files, numbers, movements) follow the same contract for their small tables.

## Implementation status

| Item | State |
| --- | --- |
| Slice folder + rules | ✅ Documented |
| `ui/DocumentsPage.tsx` | ✅ Implemented (Module 03 Phase 2) |
| `ui/DocumentDetailsPage.tsx` | ⏳ Phase 3 |
| Browser verification | ⏳ Pending — the open browser has no signed-in session; registry filters/search/create against the live API still need an interactive pass after sign-in. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Module 03 Phase 2: implemented `ui/DocumentsPage.tsx` (`DOCUMENT_VIEW` gate + `widgets/document-registry-table`); detail page deferred to Phase 3, so no `/documents/:documentId` route is registered yet. |
| 2026-10-25 | Buffy | Documented the shared data-grid contract for the registry and detail lists (column chooser, row/header context menus). |
| 2026-10-25 | Cursor | Documented Module 06 attachments panel on detail, URL tabs, and composed widgets. |
| 2026-10-22 | opencode | Converted this README from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Created `pages/documents` slice folder and README. |
