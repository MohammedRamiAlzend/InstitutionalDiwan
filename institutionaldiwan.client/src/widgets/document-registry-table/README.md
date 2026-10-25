# `src/widgets/document-registry-table` — Document Registry Table & Filter Bar Widget
## 1. Purpose
Composes the main institutional correspondence registry grid through `shared/ui/data-table`, combining filter controls (direction, type, security level, date range, search query), pagination, `entities/document` status badges, a column chooser, and a permission-filtered row/header context menu with actions from `features/document-create` and `features/document-access-grant`. Table id: `documents`.
## 2. Strict FSD Rules
- Allowed Imports: `src/features/document-create`, `src/features/document-access-grant`, `src/entities/document`, `src/entities/lookup`, `src/entities/department`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, or sibling `src/widgets/*` slices.
- Public API: Export `DocumentRegistryTable` and `DocumentFilterBar` only via `src/widgets/document-registry-table/index.ts`.
## 3. Planned Contents & Segments
- `ui/DocumentRegistryTable.tsx` — Server-paginated RTL data table of correspondence records.
- `ui/DocumentFilterBar.tsx` — Search and lookup filter toolbar.
- `model/use-document-filters.ts` — URL search-param hook for filters, sorting, and paging (no local filter state).
- `model/columns.ts` — Default column set plus the stable `tableId: 'documents'` consumed by the column chooser.
- `model/row-menu.ts` — Permission-filtered menu item groups for the row, header, and empty-area context menus.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-table`, `@tanstack/react-query`, `lucide-react`, `date-fns`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `DocumentRegistryTable` (via `shared/ui/data-table`) and `DocumentFilterBar`
- Column visibility persists per browser in `localStorage`; it is never sent to the API or stored in the URL
- Backend support landed 2026-10-22: `GET /api/documents` accepts `page`, `pageSize`, `search`, `sortBy`, `sortDescending`, `statusId`, `owningDepartmentId`, `directionId`, `documentTypeId` and returns `{ items, totalCount, totalPages, hasPrevious, hasNext }`, so the table can drive filters and paging from URL search params without client-side slicing.
## 6. Change Log
- 2026-10-25: Documented the shared data-grid contract: URL-only filters/sort/paging, `localStorage` column chooser (`documents`), and permission-filtered row/header context menus.
- 2026-10-22: Recorded that the backend now serves the server-paged `/api/documents` contract this widget needs.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `widgets/document-registry-table` slice folder and `README.md`.
