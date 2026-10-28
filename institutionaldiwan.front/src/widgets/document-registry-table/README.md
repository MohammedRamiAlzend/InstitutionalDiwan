# `src/widgets/document-registry-table` — Correspondence Registry Grid

> **Status:** ✅ Implemented (Module 03 Phase 2, 2026-10-28) — server-paged document registry with
> URL filters, seeded lookup labels, department resolution, column chooser, and row/header/empty
> context menus.

## 1. Purpose

Composes the institutional correspondence registry through `shared/ui/data-table`. The grid reads
`GET /api/documents` with server paging, search (title/summary/official number), sorting
(`createdAt`, `title`, `registrationDate`, `officialNumber`), and the four server filters
(`statusId`, `directionId`, `documentTypeId`, `owningDepartmentId`). It mounts
`features/document-create`'s dialog from the toolbar and menu entries when the caller holds
`DOCUMENT_CREATE`.

## 2. Strict FSD Rules

- Allowed Imports: `@/features/document-create`, `@/entities/document`, `@/entities/department`,
  `@/entities/lookup`, `@/entities/user`, and `@/shared/*`.
- Forbidden Imports: NEVER import from `@/app`, `@/pages`, or sibling `@/widgets/*` slices.
- Public API: export `DocumentRegistryTable` only via `src/widgets/document-registry-table/index.ts`.
- No `useMutation` here; the create dialog owns the write.

## 3. Contents

| File | Purpose |
| --- | --- |
| `ui/DocumentRegistryTable.tsx` | Data grid: title/summary, official number (`dir="ltr"`), direction and status badges, type/confidentiality/priority lookup labels, owning department, document/registration/creation dates, status/direction/type/department filter bar, copy row actions, and create/refresh menus. |
| `index.ts` | Public barrel. |

## 4. Conventions

- Table id is `documents`, so column visibility persists as
  `diwan.table.documents.columns.v1`.
- Filter/search/sort/page state lives in the URL through `useTableState`
  (`statusId`, `directionId`, `documentTypeId`, `owningDepartmentId` are the filter keys).
- Department labels come from `GET /api/identity-options/departments`, which now also accepts
  `DOCUMENT_VIEW`; unresolved ids fall back to the compact short id.
- Row actions are permission-aware: only `DOCUMENT_CREATE` sees the create entries.
- Detail-page navigation is added with `pages/documents`' detail route in Phase 3.

## 5. Implementation Status

| Item | State |
| --- | --- |
| Data grid, filters, sorting, paging | ✅ Implemented |
| Column chooser + persisted visibility | ✅ Implemented (`documents`) |
| Row/header/empty context menus + overflow trigger | ✅ Implemented |
| Create dialog integration | ✅ Implemented |
| Browser verification | ⏳ Pending — the open browser has no signed-in session, so the list/filters and both creation modes still need an interactive pass after sign-in. |

## 6. Change Log

- 2026-10-28: «مسح البحث والمرشحات» now calls `useTableState().clearQuery()`, so it removes the
  search and the filters together; calling `setSearch('')` and `clearFilters()` back to back left the
  search in the URL.
- 2026-10-28: Module 03 Phase 2 — implemented the registry grid over `GET /api/documents`,
  department/status/direction/type filters, seeded lookup labels, copy actions, and the
  `CreateDocumentDialog` integration. Table id is `documents`.
- 2026-10-25: Documented the shared data-grid contract: URL-only filters/sort/paging,
  `localStorage` column chooser (`documents`), and permission-filtered row/header context menus.
- 2026-10-22: Recorded that the backend now serves the server-paged `/api/documents` contract this
  widget needs.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown.
- 2026-10-19: Created `widgets/document-registry-table` slice folder and `README.md`.
