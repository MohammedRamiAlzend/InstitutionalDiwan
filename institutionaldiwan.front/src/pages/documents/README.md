# `src/pages/documents` — Correspondence Registry & Document Details (`/documents`)

> **Status:** Docs only — backend Modules 03 + 06 ready; page code pending.

## Purpose

Renders the correspondence registry at `/documents` and document detail at `/documents/:documentId`.
Detail composes metadata, numbering, access grants, movement timeline, and the **attachments panel**
(Module 06).

## Strict FSD Rules

- Allowed: `@/widgets/*`, `@/features/*`, `@/entities/*`, `@/shared/*`.
- Forbidden: `@/app/*`, sibling pages.
- Public API: export `DocumentsPage` and `DocumentDetailsPage` via `index.ts`.

## Planned Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentsPage.tsx` | Registry + create document entry point. |
| `ui/DocumentDetailsPage.tsx` | Tabs/sections: details, participants, numbers, movements, files, grants. |
| `index.ts` | Public barrel. |

## Widgets / features composed

- `widgets/document-registry-table`
- `widgets/document-attachments-panel` ← `GET/POST /api/documents/{id}/files`
- `widgets/movement-timeline`
- `features/document-create`, `document-access-grant`, `attachment-upload`, `movement-routing`

## URL state

- List: `?page=&pageSize=&search=&statusId=&directionId=&documentTypeId=&sortBy=&sortDescending=`
- Detail: `?tab=details|files|movements|grants`

## Table contract

The registry mounts `widgets/document-registry-table`, which uses the shared data grid: URL-driven
filters/sort/paging, a column chooser persisted under the `documents` table id, permission-filtered
row and header context menus, and an always-visible row overflow trigger. Detail tabs that show
lists (files, numbers, movements) follow the same contract for their small tables.

## Implementation status

| Item | State |
| --- | --- |
| Slice folder + rules | ✅ Documented |
| Page components | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Documented the shared data-grid contract for the registry and detail lists (column chooser, row/header context menus). |
| 2026-10-25 | Cursor | Documented Module 06 attachments panel on detail, URL tabs, and composed widgets. |
| 2026-10-22 | opencode | Converted this README from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Created `pages/documents` slice folder and README. |
