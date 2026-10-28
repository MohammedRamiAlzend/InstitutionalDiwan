# `src/widgets/department-hierarchy-tree` — Department Hierarchy Widget
> **Status:** Module 01 hierarchy UI implemented: native department/employee hierarchy, Mermaid department diagram, department-type distribution chart, flat department grid, department management integration, and paged employee rosters with read-only employee details. Browser verification remains pending.

## 1. Purpose
Renders the Module 01 organizational hierarchy (`entities/department`) as a collapsible native list tree, a Mermaid diagram, a department-type distribution chart, or a flat grid. Department search stays in `?search=` and the selected view is `?view=tree|mermaid|chart|table` (tree is the default). The tree, diagram, and chart use the same filtered tree, including the ancestors retained for search hits.

Existing create/edit/parent-change and activation actions are composed from `features/department-manage`; this completion preserves those pending integrations. Opening a department shows its data and an employee roster. Employee actions in this slice are read-only: details and copying name, number, or id; employee administration remains outside this widget.

## 2. Strict FSD Rules
- Allowed imports: `features/department-manage`, `entities/department`, `entities/employee`, `entities/user` (session/permission check), `entities/lookup`, and `shared/*`, through their public barrels.
- Forbidden imports: `app`, `pages`, and sibling `widgets/*` slices.
- Public API: exports `DepartmentHierarchyTree` via the slice's `index.ts`; internal composing components are not public exports.
- Mermaid source is built by `departmentTreeToMermaid` in the department entity. The diagram contains departments only; lazily loaded employee pages are not exported to Mermaid.
- Logical spacing, mirrored directional icons, shared menu/dialog primitives, and semantic color tokens support Arabic/RTL, LTR, light, and dark layouts. This update does not claim browser verification of those modes.

## 3. Contents & Segments
| File | Purpose |
| --- | --- |
| `ui/DepartmentHierarchyTree.tsx` | Tree/Mermaid/chart/table switcher, department search, expansion controls, optional employee branches, shared hierarchy loading/error/retry/empty/no-match handling, department details, and existing create/edit/activation dialog orchestration. |
| `ui/DepartmentTreeNodeRow.tsx` | Recursive native list row with expansion controls, department badge/type/child count, detail access, identical context/overflow menus, and lazily opened direct-employee branches. |
| `ui/DepartmentTypeChart.tsx` | Dependency-free distribution of visible departments by type, with textual counts/percentages and decorative token-styled bars; uses only the loaded filtered hierarchy. |
| `ui/DepartmentTable.tsx` | Flat client-paged `DataTable` (`tableId="departments"`): sorting, column chooser, row/header/empty menus, and department manage callbacks. |
| `ui/DepartmentDetailDialog.tsx` | Department details from `GET /api/departments/{id}`, edit access, copy actions, manager lookup, and the employee roster. |
| `ui/DepartmentEmployeesSection.tsx` | Permission-gated roster and optional tree branch: department-scoped employee reads, namespaced URL search/paging/descendant state, refresh/retry, context/overflow row actions, and employee-detail selection. |
| `ui/DepartmentEmployeeDetailDialog.tsx` | Read-only employee details from `useEmployeeById` / `GET /api/employees/{id}`, gated by `EMPLOYEE_MANAGE`, with loading/error/retry handling. |
| `ui/DepartmentSearch.tsx` | Reusable 300 ms debounced department/employee search; committed terms stay in the URL and external URL changes cancel pending drafts. |
| `model/departmentTree.ts` | View validation (including `chart`), department matching/filtering, ancestor retention, id collection, and counting. |
| `model/clipboard.ts` | Arabic-toast clipboard helper used for department and employee values. |
| `index.ts` | Public widget barrel. |

## 4. Data Loading & Dependencies
- Employee endpoints require `EMPLOYEE_MANAGE`. Without it, neither employee branches nor rosters/details are rendered or requested.
- Native employee branches are optional: enable «إظهار فروع الموظفين», then open «الموظفون المباشرون» under an expanded department. Merely enabling the option or expanding all departments does not request employee lists. Closing the employee/department branch unmounts its roster.
- Tree employee requests always use `departmentId`, `includeSubDepartments=false`, `page`, and `pageSize=20`. No organization-wide employee request or automatic fetch-all loop is used.
- Dialog rosters also send `departmentId` and page in groups of 20; descendants are included by default and can be excluded. Previous/next controls reach every server page, removing the former first-50 truncation. Search is server-side name/number search; filters reset that roster's page.
- Roster URL keys are `employees.roster.<departmentId>.search|page|descendants`; tree keys use `employees.tree.<departmentId>.search|page`. They do not overwrite the main department search or paging. Default values are omitted. Tree branch visibility is local view state.
- Previous-page placeholder data is hidden while a new page/filter loads. Successful responses clamp out-of-range pages; errors offer retry and a first-page recovery action when applicable.
- Chart counts include visible search ancestors and do not represent employee totals. The caption states this scope. Tree, Mermaid, and chart share loading, API-error/retry, empty, and no-match states, so a failed hierarchy request cannot render an empty diagram.
- Existing packages only: React Query, React Router, Lucide, shared Radix primitives, Sonner, and the shared lazy Mermaid renderer. No dependencies or entity APIs were added.

## 5. Implementation Status
| Item | State |
| --- | --- |
| Native department hierarchy and identical context/overflow actions | Implemented; department collapse controls are disabled while search forces matching paths open. |
| Optional direct-employee branches | Implemented with permission gating, explicit lazy expansion, search, paging, menus, and details. |
| Mermaid department diagram | Implemented with activatable department nodes; syntax/theme handling remains in the shared renderer. |
| Department-type chart (`view=chart`) | Implemented from the loaded filtered tree, with accessible textual values and semantic tokens. |
| Shared tree/diagram/chart request states | Implemented. |
| Department detail employee roster beyond 50 results | Implemented with server search/paging and optional descendants. |
| Existing department create/edit/activation integration | Preserved; mutations and forms remain in `features/department-manage`. |
| Department grid paging/sort/search/column preferences | Existing implementation preserved. |
| Browser interaction, RTL/LTR, and light/dark verification | Pending main-agent browser validation; not claimed by this update. |

## 6. Change Log
- 2026-10-28: Complete scoped hierarchy UI with explicitly lazy, permission-gated employee branches; scoped roster search/paging and employee detail/context/overflow actions; token-based department-type chart; shared hierarchy request states; and department overflow parity. Preserve pending department manage actions. `npm run build` passed with chunk-size warnings; `npm run lint` passed with one out-of-scope settings-form warning. Browser validation is pending.
- 2026-10-28: Diagram nodes are now activatable: clicking (or `Enter` / `Space` on) a department in the Mermaid view opens the same detail dialog with its employee roster, resolved through the entity's new `departmentIdByNodeId` map; a hint line above the diagram announces it.
- 2026-10-28: Added `DepartmentEmployeesSection` to the detail dialog — clicking a department now lists the people assigned to it, with an «تضمين الأقسام الفرعية» toggle backed by the new `departmentId`/`includeSubDepartments` employee filters.
- 2026-10-28: Implemented Phase 1 (read-only): tree/mermaid/table switcher, shared URL search, node and row context menus, detail dialog, tree model helpers, and docs. Build and lint pass.
- 2026-10-25: Added node context menus and the tree / Mermaid / chart switcher (departments and employees) to the planned contents.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `widgets/department-hierarchy-tree` slice folder and `README.md`.
