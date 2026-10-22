# institutionaldiwan.client / src/widgets — Composite UI Widgets Layer
> **Status:** Scaffolding & Rules — FSD widget slices and composition rules defined; code pending.
## Purpose
`src/widgets/` contains standalone, self-contained page blocks that combine one or more `entities` (read-only domain views) with `features` (interactive actions/mutations) into a complete, reusable UI region—such as the RTL application shell, the correspondence registry data table with inline actions, or the interactive organizational hierarchy tree.
## Layer & Import Rules (FSD Strict Hierarchy)
`widgets` sits below `pages` and above `features` (app → pages → widgets → features → entities → shared):
- Allowed imports: `@/features/*`, `@/entities/*`, `@/shared/*` (via each slice's `index.ts` public barrel).
- Forbidden imports: Cannot import from `@/app/*`, `@/pages/*`, or another widget in `@/widgets/*`.
- Public API rule: Each widget slice (`widgets/<slice>/`) must export its root widget component via `widgets/<slice>/index.ts`.
## Required Packages
| Package | Version / Role | Used in `src/widgets/` for |
| --- | --- | --- |
| `@tanstack/react-table` | `^8.x` | `document-registry-table/`, sortable/filterable RTL institutional tables |
| `lucide-react` | `^1.x` | Navigation, tree expand/collapse, status, and toolbar icons |
| `@radix-ui/react-dropdown-menu`, `@radix-ui/react-dialog`, `@radix-ui/react-Navigation-menu` | `^1.x` / `^2.x` | Accessible RTL menus, drawers, and action toolbars |
## Planned Slices & Contents
| Slice Folder | Purpose | Composes (`entities` + `features`) | State |
| --- | --- | --- | --- |
| `app-shell/` | Main authenticated RTL layout: collapsible sidebar filtered by user `Permissions`, top bar with user department/scope badge, and logout/password actions. | `entities/user` + `features/auth` + `widgets/notification-center` | ❌ Planned |
| `document-registry-table/` | Correspondence data grid showing official number, year, title, direction, status badge, registration date, and action menu. | `entities/document` + `entities/lookup` + `features/document-create` + `features/document-access-grant` | ❌ Planned |
| `department-hierarchy-tree/` | Interactive recursive tree view of `GET /api/departments/tree` with inline create child, edit, reparent (`MoveDepartment`), and activate/deactivate triggers. | `entities/department` + `features/department-manage` | ❌ Planned |
| `movement-timeline/` | Vertical chronological step chain displaying document handovers via Central Diwan, receiver status, and routing actions. | `entities/movement` + `features/movement-routing` | ❌ Planned |
| `notification-center/` | Real-time SignalR notification bell, unread badge, and slide-over drawer. | `entities/user` + SignalR hub | ❌ Planned |
## Conventions / Vibe-Coding Notes (OpenCode & Freebuff)
- No direct mutation logic inside a widget: A widget renders data from `entities/*` queries and mounts trigger buttons/modals from `features/*`. Do not write `useMutation` calls directly inside `widgets/*`.
- RTL layout discipline: Use Tailwind logical properties (`ms-*`, `me-*`, `ps-*`, `pe-*`, `border-s`, `border-e`, `text-start`, `text-end`) so the sidebar, tables, and trees flip cleanly between Arabic (`rtl`) and English (`ltr`).
- Golden Rule: Update this `README.md` and the touched widget slice's `README.md` in the same turn whenever files change.
## Implementation status
| Widget Slice | State |
| --- | --- |
| `app-shell/` | ❌ Not implemented |
| `document-registry-table/` | ❌ Not implemented |
| `department-hierarchy-tree/` | ❌ Not implemented |
| `movement-timeline/` | ❌ Not implemented |
| `notification-center/` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/widgets` FSD layer folder and documented widget slices, rules, and packages. |
