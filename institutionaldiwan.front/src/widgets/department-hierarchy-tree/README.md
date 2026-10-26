# `src/widgets/department-hierarchy-tree` — Interactive Department Tree Widget
## 1. Purpose
Renders the organizational hierarchy of departments (`entities/department`) as an interactive, collapsible RTL tree view with inline edit/create triggers powered by `features/department-manage`. Each node exposes a permission-filtered right-click menu (add child, rename, reparent, deactivate, copy code/id, expand subtree), and the surface offers a **tree / Mermaid / chart** view switcher through `shared/ui/mermaid-view` so departments and their employees can also be read as a Mermaid diagram.
## 2. Strict FSD Rules
- Allowed Imports: `src/features/department-manage`, `src/entities/department`, `src/entities/lookup`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, or sibling `src/widgets/*` slices.
- Public API: Export `DepartmentHierarchyTree` only via `src/widgets/department-hierarchy-tree/index.ts`.
## 3. Planned Contents & Segments
- `ui/DepartmentHierarchyTree.tsx` — Recursive tree view displaying department code, Arabic name, type badge, node context menu, and action buttons.
- `ui/DepartmentTreeNode.tsx` — Individual node item component with focusable menu trigger (`Shift+F10`).
- `ui/DepartmentViewSwitcher.tsx` — Toggles tree / Mermaid diagram / chart through `shared/ui/mermaid-view`, fed by the entity's `toTree` / `toMermaid` builders.
- `model/node-menu.ts` — Permission-filtered menu item groups shared by the tree nodes.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `lucide-react`, `clsx`; `mermaid` arrives through `shared/ui/mermaid-view` (lazy-loaded)
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `DepartmentHierarchyTree` and the node context menu
- Implement the tree / Mermaid / chart switcher for departments and their employees
## 6. Change Log
- 2026-10-25: Added node context menus and the tree / Mermaid / chart switcher (departments and employees) to the planned contents.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `widgets/department-hierarchy-tree` slice folder and `README.md`.
