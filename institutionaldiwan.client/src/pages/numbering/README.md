# `src/pages/numbering` — Numbering Registry & Sequences Page Slice (`/numbering`)
## 1. Purpose
Renders the numbering sequences administration page at `/numbering`, displaying active fiscal-year serial counters (`entities/numbering-registry`) per department and document direction (وارد / صادر / داخلي).

The counters table uses the shared data grid: filters/search/paging in the URL, a column chooser
persisted under the `numbering-registries` table id, and permission-filtered (`NUMBERING_MANAGE`)
row and header context menus for create/activate/deactivate and copy actions.
## 2. Strict FSD Rules
- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `NumberingPage` only via `src/pages/numbering/index.ts`.
## 3. Planned Contents & Segments
- `ui/NumberingPage.tsx` — Table and filter view of `NumberingRegistry` counters by year and department.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-table`, `@tanstack/react-query`, `lucide-react`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `NumberingPage`
## 6. Change Log
- 2026-10-25: Documented the shared data-grid and context-menu requirements for the counters table.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/numbering` slice folder and `README.md`.
