# `src/pages/departments` — Organizational Hierarchy Page Slice (`/departments`)
## 1. Purpose
Renders the department management screen at `/departments`, composing `DepartmentHierarchyTree` (`widgets/department-hierarchy-tree`) and `DepartmentFormDialog` (`features/department-manage`).
## 2. Strict FSD Rules
- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `DepartmentsPage` only via `src/pages/departments/index.ts`.
## 3. Planned Contents & Segments
- `ui/DepartmentsPage.tsx` — Page layout with search/filter header and `DepartmentHierarchyTree`.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `lucide-react`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `DepartmentsPage`
## 6. Change Log
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/departments` slice folder and `README.md`.
