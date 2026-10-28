# `src/widgets/department-hierarchy-tree` — Department Hierarchy Widget
> **Status:** ✅ Read-only Phase 1 implemented (2026-10-28) — native RTL tree with node context menus, Mermaid `flowchart TD` view with activatable nodes, flat grid, URL-driven search/view, a read-only detail dialog, and the department's employee roster inside that dialog. Manage actions land with `features/department-manage`.
## 1. Purpose
Renders the Module 01 organizational hierarchy (`entities/department`) as an interactive, collapsible RTL tree, a Mermaid diagram, or a flat grid. All three views share the same URL search term (`?search=`) and the same data; the selected view is `?view=tree|mermaid|table` (tree is the default, so the parameter is omitted for it).

Phase 1 is read-only: create, edit, reparent, and deactivate actions belong to `features/department-manage` and are intentionally absent here. Opening a department shows its data and the employees who belong to it.
## 2. Strict FSD Rules
- Allowed Imports: `src/entities/department`, `src/entities/employee`, `src/entities/user` (session/permission check), `src/entities/lookup`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, `src/features`, or sibling `src/widgets/*` slices.
- Public API: exports `DepartmentHierarchyTree` via `src/widgets/department-hierarchy-tree/index.ts`.
- The Mermaid source is built by the entity (`departmentTreeToMermaid`), never here; the widget only composes views.
## 3. Contents & Segments
| File | Purpose |
| --- | --- |
| `ui/DepartmentHierarchyTree.tsx` | Surface: view switcher (شجرة / مخطط / جدول), tree/mermaid search, expand-all/collapse-all, refresh, loading skeleton, Arabic error + retry, empty and no-match states, and the detail dialog state. Diagram nodes open the same detail dialog (the entity's `departmentIdByNodeId` resolves the clicked Mermaid node back to its department). |
| `ui/DepartmentTreeNodeRow.tsx` | Recursive tree row: expand/collapse trigger, department badge with LTR code, type label, «الديوان المركزي» badge, direct-child count, click-to-details, and a permission-filtered context menu reachable with `Shift+F10` / the context-menu key. |
| `ui/DepartmentTable.tsx` | Flat client-paged grid on the shared `DataTable` (stable `tableId="departments"`): sortable columns (name, type, parent, status, created), hidden-by-default external flags/manager/notes/timestamps, column chooser, row/header/empty menus. |
| `ui/DepartmentDetailDialog.tsx` | Read-only details from `GET /api/departments/{id}`: code, both names, type lookup label, parent, manager, status, external send/receive, notes, and timestamps, plus copy-code/copy-id actions and the employee roster. |
| `ui/DepartmentEmployeesSection.tsx` | The department's roster from `GET /api/employees?departmentId=…&includeSubDepartments=…`: count badge, «تضمين الأقسام الفرعية» checkbox (default on), rows with name/job title/number/status, and loading/error/empty states. Hidden without `EMPLOYEE_MANAGE` because the endpoint is gated on it. |
| `ui/DepartmentSearch.tsx` | Debounced (300 ms) search input used by the tree and Mermaid views; the committed term stays in the URL. |
| `model/departmentTree.ts` | `DepartmentView`, `isDepartmentView`, `matchesDepartment`, `filterDepartmentTree` (keeps ancestors of hits), `collectDepartmentIds`, and `countDepartments`. |
| `model/clipboard.ts` | Arabic-toast clipboard helper for department codes and ids. |
| `index.ts` | Public API barrel file. |
## 4. Required Packages
- `@tanstack/react-query`, `lucide-react`, `clsx`, `sonner` — ✅ installed
- `mermaid` — arrives only through `shared/ui/mermaid-view` (lazy-loaded) — ✅ installed
## 5. Implementation Status
- ✅ Tree view with collapse state (paused during search so every matching path stays visible), expand-all/collapse-all, per-node context menus, and keyboard access.
- ✅ Mermaid view fed by `departmentTreeToMermaid(filteredTree)`; theme re-render and invalid-syntax handling come from `shared/ui/mermaid-view`. Each flowchart node is activatable (click, `Enter`, `Space`) and opens the department's detail dialog, header and employee roster included; a hint above the diagram states this.
- ✅ Detail dialog lists the department's employees through `entities/employee` with a «تضمين الأقسام الفرعية» toggle (on by default), a count badge, and loading/error/empty states.
- ✅ Table view with URL paging/sort/search and persisted column preferences (`diwan.table.departments.columns.v1`).
- ✅ Read-only detail dialog with query error retry and the department's employee roster (first 50, optional descendants).
- ❌ Manage actions (add child, rename, reparent, deactivate), employee nodes inside the tree itself, and the chart view remain future work.
## 6. Change Log
- 2026-10-28: Diagram nodes are now activatable: clicking (or `Enter` / `Space` on) a department in the Mermaid view opens the same detail dialog with its employee roster, resolved through the entity's new `departmentIdByNodeId` map; a hint line above the diagram announces it.
- 2026-10-28: Added `DepartmentEmployeesSection` to the detail dialog — clicking a department now lists the people assigned to it, with an «تضمين الأقسام الفرعية» toggle backed by the new `departmentId`/`includeSubDepartments` employee filters.
- 2026-10-28: Implemented Phase 1 (read-only): tree/mermaid/table switcher, shared URL search, node and row context menus, detail dialog, tree model helpers, and docs. Build and lint pass.
- 2026-10-25: Added node context menus and the tree / Mermaid / chart switcher (departments and employees) to the planned contents.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `widgets/department-hierarchy-tree` slice folder and `README.md`.
