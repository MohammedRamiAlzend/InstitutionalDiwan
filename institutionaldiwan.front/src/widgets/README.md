# institutionaldiwan.front / src/widgets — Composite UI Widgets Layer

> **Status:** `app-shell`, Identity admin tables, the Module 01 department hierarchy, and the
> Module 03 document registry are implemented. The hierarchy includes department writes, lazy
> employee branches, paged rosters, and a chart. Final browser verification and Module 02 audit fixes
> remain in progress. Remaining Module 03 widgets are documentation only. `/change-password` is
> standalone.

## Purpose

`src/widgets/` contains self-contained page blocks that combine `entities` (reads) with `features`
(writes) into reusable UI regions — app shell, registry tables, inbox, attachments panel, audit
browser.

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/features/*`, `@/entities/*`, `@/shared/*` (public barrels).
- Forbidden: `@/app/*`, `@/pages/*`, sibling widgets.
- Export only the root widget via `index.ts`.

## Contents — Implemented and Planned Slices

| Slice Folder | Purpose | Composes | State |
| --- | --- | --- | --- |
| `app-shell/` | RTL layout, permission-filtered navigation including institution settings, header, shell/user menus, local display settings, logout | `entities/user` + `features/auth` | Implemented. |
| `users-table/` | Users administration grid with create/edit/roles/access dialogs and status confirmation | `entities/{user,department,role}` + `features/user-manage` | ✅ Implemented |
| `roles-table/` | Roles administration grid with create/edit, permission matrix, and status confirmation | `entities/role` + `features/role-manage` | ✅ Implemented |
| `employees-table/` | Employees administration grid with create/edit and status confirmation | `entities/{employee,department}` + `features/employee-manage` | ✅ Implemented |
| `document-registry-table/` | Correspondence grid with URL filters (status/direction/type/department), paging/sorting, lookup labels, and create entry points | `entities/document` + `entities/lookup` + `entities/department` + `features/document-create` | ✅ Implemented (Phase 2) |
| `document-attachments-panel/` | File list, upload, download, verify checksum, archive | `entities/document-file` + `features/attachment-upload` | ❌ Planned (API live) |
| `department-hierarchy-tree/` | Tree/Mermaid/chart/grid, department manage actions, lazy direct-employee branches, paged/searchable rosters, employee details and context/overflow menus | `features/department-manage` + department/employee/lookup/user entities | Implemented, including shared tree/diagram/chart request states; final browser verification pending. |
| `movement-inbox/` | Inbox/outbox table with routing action menu | `entities/movement` + `features/movement-routing` | ❌ Planned (API live) |
| `movement-timeline/` | Vertical step chain for one document | `entities/movement` + `features/movement-routing` | ❌ Planned |
| `audit-log-table/` | Filterable audit grid + hash columns + integrity toolbar | `entities/audit-log` + `features/audit-manage` | ❌ Planned (API live) |
| `visualization-panel/` | Mermaid source viewer plus data-derived diagram / chart / tree switcher with node context menus | `entities/department`, `entities/movement` + `shared/ui/mermaid-view` | ❌ Planned |
| `notification-center/` | SignalR bell/drawer | Deferred until Module 10 + hubs | ❌ Blocked |

## Conventions

- No `useMutation` inside widgets — mount feature UI triggers instead.
- Logical Tailwind props only (`ms-*` / `me-*` / `ps-*` / `pe-*`).
- Every table widget uses `shared/ui/data-table`: URL-driven filters/search/sort/pagination and a
  column chooser whose visibility is saved per table in `localStorage` (`diwan.table.<tableId>.columns.v1`).
  No bespoke `<table>` markup.
- Every row, header, empty area, tree node, and visualization node exposes a permission-filtered
  context menu through `shared/ui/context-menu`, mirrored by an always-visible overflow trigger.
- Visualization widgets compose `shared/ui/mermaid-view`; data → Mermaid/tree builders stay in the
  owning entity's `model/`.
- The shell is the only widget mounted by the router; slices with no page yet appear in its sidebar
  with `available: false` and the Arabic «قريبًا» badge instead of linking to 404.

## Implementation status

| Widget Slice | State |
| --- | --- |
| `app-shell/` | ✅ Implemented and verified in-browser (Phase 1) |
| `users-table/` | ✅ Implemented and verified in-browser (Phase 3) |
| `roles-table/` | ✅ Implemented and verified in-browser (Phase 4) |
| `employees-table/` | ✅ Implemented and verified in-browser (Phase 5) |
| `department-hierarchy-tree/` | Implemented: management, optional lazy employee branches, paged/searchable rosters and read-only details/menus, department-type chart, shared request states; final browser verification pending. |
| `document-registry-table/` | ✅ Implemented (Phase 2; interactive browser pass pending a signed-in session) |
| `document-attachments-panel/`, `movement-inbox/`, `movement-timeline/`, `audit-log-table/` | ❌ Not implemented (backend live) |
| `visualization-panel/` | ❌ Not implemented (`shared/ui/mermaid-view` is now live, so the cross-module switcher can follow) |
| `notification-center/` | ❌ Blocked on Module 10 / SignalR |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | Qoder | Module 03 Phase 2: implemented `document-registry-table` (server paging/search/sort, status/direction/type/department filters, seeded lookup labels, document/draft creation entry points). |
| 2026-10-28 | agent | Refresh Module 01 hierarchy coverage for department writes, lazy employee branches, paged rosters/details/menus, chart, and shared request states; final verification remains pending. |
| 2026-10-28 | agent | Diagram nodes in `department-hierarchy-tree` are now activatable (click / `Enter` / `Space` via `shared/ui/mermaid-view`'s `onNodeClick`) and open the department's detail dialog, alongside the existing tree-node and grid-row paths. |
| 2026-10-28 | agent | `department-hierarchy-tree` detail dialog now lists the department's employees (`DepartmentEmployeesSection`, `entities/employee`) with a «تضمين الأقسام الفرعية» toggle, permission gate, and loading/error/empty states; employee nodes inside the tree and the chart view stay pending. |
| 2026-10-28 | agent | Implemented `department-hierarchy-tree` for Module 01 Phase 1 (read-only tree / Mermaid / table with URL-driven search and view, node context menus, collapse-all, and a detail dialog) and documented the slice. |
| 2026-10-28 | agent | Implemented `employees-table` for Phase 5 (server search/paging, department resolution/fallback, permission-filtered menus, status confirmation, create/edit dialog) and documented the slice. |
| 2026-10-28 | agent | Implemented `roles-table` for Phase 4 (client search/sort/paging, permission-filtered row/header/empty menus, role and permission-matrix dialogs, system-role protection) and documented the slice. |
| 2026-10-28 | agent | Implemented `users-table` for Phase 3 (users grid, permission-filtered row/header/empty menus, status confirmation) and documented the slice. |
| 2026-10-27 | agent | Implemented `app-shell` for Phase 1 (permission-filtered sidebar, header/account menus, mobile drawer, shell/item context menus, settings dialog with theme + density) and documented the «قريبًا» convention for unavailable slices. |
| 2026-10-25 | Buffy | Documented the mandatory table/context-menu conventions and added the `visualization-panel` widget (Mermaid + data-derived tree/chart views); upgraded the department tree with node menus and a view switcher. |
| 2026-10-25 | Cursor | Added attachments panel, movement inbox, and audit-log table widgets; marked notification-center blocked. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/widgets` FSD layer folder and documented widget slices, rules, and packages. |
