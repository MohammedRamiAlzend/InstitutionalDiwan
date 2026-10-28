# `src/pages/departments` — Organizational Hierarchy Page Slice (`/departments`)

> **Status:** Implemented (Module 01 Phase 1, 2026-10-28) — read-only. The `/departments` screen gates
> on `DEPARTMENT_MANAGE` and mounts the department hierarchy widget (tree / Mermaid / table), whose
> detail dialog lists the employees of the selected department.

## 1. Purpose

Renders the organizational hierarchy route. The page is a thin composition unit: it owns the
route-level permission gate and the Arabic page header, and delegates the hierarchy tree, the Mermaid
diagram, the tabular view, the search box, and the detail dialog (department data plus its roster)
to `widgets/department-hierarchy-tree`.

## 2. Strict FSD Rules

- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `DepartmentsPage` only via `src/pages/departments/index.ts`.

## 3. Contents & Segments

- `ui/DepartmentsPage.tsx` — `RequirePermission(DEPARTMENT_MANAGE)` wrapper, page header
  («الأقسام التنظيمية»), and `<DepartmentHierarchyTree />`. The view switcher (`view=tree|mermaid|table`)
  and the search term live in URL search params, owned by the widget's `useTableState`.
- `index.ts` — Public API barrel file.

## 4. Required Packages

- `@tanstack/react-query` (through the widget), `lucide-react` (header and widget icons),
  `mermaid` (lazy, only when the diagram view is opened)

## 5. Implementation Status

- ✅ `/departments` route mounted inside the app shell and gated on `DEPARTMENT_MANAGE` (Arabic
  forbidden state otherwise).
- ✅ Read-only hierarchy: interactive tree, Mermaid diagram (clicking a node opens the details),
  tabular view, search, node detail dialog, and the department's employee roster (optional
  sub-departments) — verified in-browser.
- ❌ Manage actions (create / edit / reparent / deactivate) belong to `features/department-manage`
  and `DepartmentFormDialog`, which land with the Module 01 write phase.
- ❌ Employee nodes inside the tree and the chart view remain pending.

## 6. Change Log

- 2026-10-28: Diagram nodes in the Mermaid view are now clickable (and keyboard-activatable) and open
  the same detail dialog as the tree and table; verified in-browser with a filtered diagram.
- 2026-10-28: The detail dialog now lists the department's employees, sourced from
  `GET /api/employees?departmentId=…&includeSubDepartments=…` (28 demo employees are seeded).
- 2026-10-28: Implemented Module 01 Phase 1 — the `/departments` page mounts `widgets/department-hierarchy-tree`
  behind the `DEPARTMENT_MANAGE` guard (read-only tree / Mermaid / table, search, detail dialog).
- 2026-10-25: Documented node context menus and the tree / Mermaid / chart view switcher for departments and employees.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/departments` slice folder and `README.md`.
