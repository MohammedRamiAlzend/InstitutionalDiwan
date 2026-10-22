# `src/widgets/department-hierarchy-tree` — Interactive Department Tree Widget
## 1. Purpose
Renders the organizational hierarchy of departments (`entities/department`) as an interactive, collapsible RTL tree view with inline edit/create triggers powered by `features/department-manage`.
## 2. Strict FSD Rules
- Allowed Imports: `src/features/department-manage`, `src/entities/department`, `src/entities/lookup`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, or sibling `src/widgets/*` slices.
- Public API: Export `DepartmentHierarchyTree` only via `src/widgets/department-hierarchy-tree/index.ts`.
## 3. Planned Contents & Segments
- `ui/DepartmentHierarchyTree.tsx` — Recursive tree view displaying department code, Arabic name, type badge, and action buttons.
- `ui/DepartmentTreeNode.tsx` — Individual node item component.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `lucide-react`, `clsx`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `DepartmentHierarchyTree`
## 6. Change Log
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `widgets/department-hierarchy-tree` slice folder and `README.md`.
