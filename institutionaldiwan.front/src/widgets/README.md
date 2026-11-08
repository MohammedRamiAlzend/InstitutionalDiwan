# institutionaldiwan.front / src/widgets — Composite UI Widgets Layer

> **Status:** `app-shell`, Identity admin tables, the Module 01 department hierarchy, the Module 03
> document registry, numbering registry, movement inbox, and external correspondents grid, the
> Module 06 attachments grid, and the Module 09 audit log/integrity tables are implemented. The
> hierarchy includes department writes, lazy employee branches, paged rosters, and a chart. The
> Module 03, Module 06 and Module 09 widgets are verified in-browser; the hierarchy's final browser
> verification and Module 02 audit fixes remain outstanding.
> Remaining widgets are documentation only. `/change-password` is standalone.

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
| `numbering-registry-table/` | Numbering registry grid (scope/specificity/format/reset/counters/status) with URL filters, duplicate-scope warning, create/edit/activate actions, and a details dialog with a next-number preview | `entities/numbering-registry` + `entities/department` + `entities/lookup` + `features/numbering-manage` | ✅ Implemented (Phase 5); filters/specificity/dedup (Module 04 Phase 2) |
| `department-register-book/` | Read-only book of numbers departments wrote in their own registers, with URL-backed department/year filters, sorting, search, and copy/open menus | `entities/numbering-registry` + `entities/department` + `entities/document` | ✅ Implemented (Module 04 Phase 3) |
| `document-attachments-panel/` | File list with URL-backed status/type filters, download of the current version, copy actions, and the row actions: رفع ملف، رفع إصدار، الإصدارات، الأرشفة، التحقق من البصمة | `entities/document-file` + `features/attachment-upload` | ✅ Grid + download + upload + versions/archive/verify (Module 06 Phases 2–4) |
| `department-hierarchy-tree/` | Tree/Mermaid/chart/grid, department manage actions, lazy direct-employee branches, paged/searchable rosters, employee details and context/overflow menus | `features/department-manage` + department/employee/lookup/user entities | Implemented, including shared tree/diagram/chart request states; final browser verification pending. |
| `movement-inbox/` | Inbox/outbox queue with search, status/type filters, overdue highlighting, and routing action menus | `entities/movement` + `features/movement-routing` | ✅ Implemented (Phase 6) |
| `external-organizations-table/` | External correspondents admin grid (server search/paging, active/all filter, status badges, copy actions) | `entities/{external-organization,lookup,user}` + `features/external-organization-manage` | ✅ Implemented (Phase 7) |
| `movement-timeline/` | Vertical step chain for one document | `entities/movement` + `features/movement-routing` | Deferred — the timeline lives in `pages/documents` until Module 05's visualization panel lands |
| `system-errors-table/` | The developer's grid of captured errors with details, Excel export and purge. | — | — |
| `audit-log-table/` | Paged audit search (hash columns, module/action/date filters) plus the integrity-report list, with checkpoint/checks toolbar | `entities/audit-log` + `features/audit-manage` | ✅ Implemented (Module 09) |
| `archive-search-table/` | Access-filtered archive search grid with a saved-searches bar | `entities/archive` + `features/archive-manage` |
| `notification-bell/` | Header bell: unread badge, live-connection line and the latest eight notifications | `entities/notification` + `features/notification-manage` + `features/notification-live` |
| `notification-inbox/` | The `/notifications` grid with server search/sort/paging and unread/type filters | `entities/notification` + `features/notification-manage` + `features/notification-live` |
| `lookup-admin/` | Catalog picker and entries grid for `/lookups` (Module 01). |
| `document-relations-panel/` | Links grid, graph/tree views and relation history for one document | `entities/document-relation` + `features/document-relation-manage` |
| `document-schema-editor/` | Schema version history and field grid of one document type with draft actions (new draft, add/edit/remove field, publish, delete) | `entities/document-schema` + `features/document-schema-manage` |
| `document-fields-panel/` | A document's dynamic fields against its pinned schema version, editable while a draft | `entities/document-schema` + `features/document-field-values` |
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
| `document-registry-table/` | ✅ Implemented and verified in-browser (Phase 2) |
| `numbering-registry-table/` | ✅ Implemented and verified in-browser (Phase 5; filters, specificity column, duplicate-scope warning, and details next-number preview added in Module 04 Phase 2) |
| `department-register-book/` | ✅ Implemented and verified in-browser (Module 04 Phase 3: replaces the bespoke register table with the shared grid) |
| `movement-inbox/` | ✅ Implemented and verified in-browser (Phase 6) |
| `external-organizations-table/` | ✅ Implemented and verified in-browser (Phase 7) |
| `audit-log-table/` | ✅ Implemented and verified in-browser (Module 09: `AuditLogTable` + `AuditIntegrityTable`, manager actions hidden for view-only roles) |
| `archive-search-table/` | ✅ Implemented (Module 07; build and lint pass, verified against the live API) |
| `notification-bell/`, `notification-inbox/` | ✅ Implemented (Module 10; build and lint pass, exercised against the live API). |
| `document-relations-panel/` | ✅ Implemented (Module 08; build and lint pass, accepted as verified by the project owner, 2026-11-03) |
| `document-schema-editor/` | ✅ Implemented and verified in-browser (dynamic fields plan Phases 1–2) |
| `document-fields-panel/` | ✅ Implemented and verified in-browser (plan Phase 4) |
| `document-attachments-panel/` | ✅ Grid, filters, download, upload, versions, archive, and verify implemented (Module 06 Phases 2–4) and browser-verified in Phase 5 across manager, `USER`, and read-only roles. |
| `movement-timeline/` | Deferred (the document movements tab renders the step chain today) |
| `visualization-panel/` | ❌ Not implemented (`shared/ui/mermaid-view` is now live, so the cross-module switcher can follow) |
| `notification-center/` | ❌ Blocked on Module 10 / SignalR |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-08 | Claude | Added `system-errors-table/`. |
| 2026-11-04 | Claude | Added `lookup-admin`. |
| 2026-11-04 | Claude | Module 10 notifications front end (plan `plans/module-10-notifications-frontend.md`): implemented `notification-bell` and `notification-inbox`. |
| 2026-11-03 | Claude | Added `document-schema-editor/` and `document-fields-panel/` for the dynamic document fields. |
| 2026-11-03 | agent | Module 08: implemented `document-relations-panel` — the relations grid, Mermaid/tree views and history. |
| 2026-11-03 | agent | Module 07: implemented `archive-search-table` — the URL-driven archive grid (location/direction/type/department/date-range filters, row and header menus) and the personal saved-searches bar. |
| 2026-11-01 | agent | Module 06 Phase 4: `document-attachments-panel` completed its row menu (الإصدارات، التحقق من البصمة for viewers; رفع إصدار، الأرشفة for a manager on a live unarchived file), renamed `canUpload` to `canManage`, and moved to one `rowAction` state that resolves the file from the live query so a mutation updates the open dialog. |
| 2026-11-01 | agent | Module 06 Phase 3: `document-attachments-panel` grew the `canUpload` prop and the «رفع ملف» toolbar/empty-state action, mounting `UploadAttachmentDialog` from `features/attachment-upload`. |
| 2026-11-01 | agent | Module 06 Phase 2: implemented `document-attachments-panel` — the client-mode files grid (`tableId="document-files"`) over `useDocumentFiles` with URL-backed status/file-type filters, ten columns (checksum and timestamps hidden by default), a row menu that downloads the current version under the server's own file name and copies the id / checksum / original name, and Arabic empty states that separate "no files" from "no match". Archived files stay listed because archiving stops versions without hiding anything. Browser verification skipped at the user's request. |
| 2026-10-29 | agent | Module 04 Phase 3: implemented `department-register-book` — the department register book on the shared client-mode grid with URL-backed department/year filters (inactive departments included), sorting, search, column chooser, and open/copy row menus; removed the bespoke table from `pages/numbering`. |
| 2026-10-29 | agent | Module 04 Phase 2: `numbering-registry-table` gained URL-backed status/reset-policy/scope filters, a scope-specificity column, the duplicate active-scope banner, and the details-dialog next-number preview with a neutral precedence explanation; verified in-browser. |
| 2026-10-29 | agent | Module 09: implemented `audit-log-table` — the URL-driven log grid (module/action/date filters, lookup labels, LTR hash pills) and the integrity-report grid, with checkpoint/check toolbar buttons, copy row actions, and `AUDIT_MANAGE`-filtered menus; verified for both the view-only and manager roles. |
| 2026-10-29 | agent | Module 03 Phase 7: implemented `external-organizations-table` (server paging/search with URL state, active/all status filter, lookup type labels, permission-filtered create/edit/status menus, and copy actions) and verified it in-browser inside the `/documents` organizations tab. |
| 2026-10-29 | agent | Module 03 Phase 6: implemented `movement-inbox` (client-mode queue over the plain-array inbox/outbox APIs, URL search/filters/sort/paging, overdue highlighting, permission-filtered routing menus) and verified it in-browser for both departments. |
| 2026-10-29 | agent | Module 03 Phase 5: implemented `numbering-registry-table` (client grid with scope/format/reset/counters/status, permission-filtered create/edit/activate menus, copy entries, details dialog) and verified it in-browser. |
| 2026-10-28 | agent | Verified `document-registry-table` in-browser (Phase 2): seeded labels, search, URL status filter, empty-state «مسح البحث والمرشحات» (now one `clearQuery` URL write), title sorting, and immediate refresh after draft/registered creation. |
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
