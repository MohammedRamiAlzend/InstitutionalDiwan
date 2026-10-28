# `src/pages/departments` — Organizational Hierarchy Page Slice (`/departments`)

> **Status:** Module 01 Organization UI implemented; final browser/build/test verification is pending.

## 1. Purpose

Thin route composition for department administration under `DEPARTMENT_MANAGE`. The page owns
its Arabic header and permission gate; `widgets/department-hierarchy-tree` owns tree / Mermaid /
chart / table views, department management dialogs, and read-only employee exploration.

## 2. Contents

| File | Purpose |
| --- | --- |
| `ui/DepartmentsPage.tsx` | Composes `RequirePermission(DEPARTMENT_MANAGE)`, the organization-management header, and `DepartmentHierarchyTree`. |
| `index.ts` | Public `DepartmentsPage` export for the router. |

## 3. Conventions / Notes

- Import lower-layer public barrels only, never `app` or sibling pages. No HTTP calls or mutations here.
- View (`view=tree|mermaid|chart|table`), department search, and grid state are URL-driven in the widget.
- Create/edit/reparent, manager assignment, notes, external policy, and active-status actions compose
  `features/department-manage`. The backend protects the configured central Diwan and inactive parents.
- Employee branches are optional and lazy: enabling them does not fetch all employees. Each opened
  branch loads only direct employees; the detail roster optionally includes descendants (default on).
  Both have server search/paging, read-only details, and context/overflow menus behind `EMPLOYEE_MANAGE`.
- The chart counts visible departments by type, including retained search ancestors, not employees.
  Tree, Mermaid, and chart share loading/error/retry/empty/no-match states.
- Institution configuration is a separate `/institution-settings` route, not a tab on this page.

## 4. Required Packages

- React Query and React Router through the widget; shared UI primitives and lazy `mermaid` rendering.

## 5. Implementation status

| Item | State |
| --- | --- |
| Shell route, page header, permission gate | Implemented. |
| Tree, Mermaid, chart, grid, search, department details | Implemented in the hierarchy widget. |
| Department write actions and manager picker | Implemented through the department-manage feature. |
| Lazy employee branches and paged/searchable detail roster | Implemented; employee reads require `EMPLOYEE_MANAGE`. |
| Final interaction, RTL/LTR, light/dark, build/test verification | Pending main-agent verification. |

## 6. Change Log

- 2026-10-28: Refresh Module 01 documentation for department management, lazy employee branches, paged roster/details/menus, the department-type chart, and shared request states; leave final verification pending.
- 2026-10-28: Diagram nodes in the Mermaid view are now clickable (and keyboard-activatable) and open
  the same detail dialog as the tree and table; verified in-browser with a filtered diagram.
- 2026-10-28: The detail dialog now lists the department's employees, sourced from
  `GET /api/employees?departmentId=…&includeSubDepartments=…` (28 demo employees are seeded).
- 2026-10-28: Implemented Module 01 Phase 1 — the `/departments` page mounts `widgets/department-hierarchy-tree`
  behind the `DEPARTMENT_MANAGE` guard (read-only tree / Mermaid / table, search, detail dialog).
- 2026-10-25: Documented node context menus and the tree / Mermaid / chart view switcher for departments and employees.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/departments` slice folder and `README.md`.
