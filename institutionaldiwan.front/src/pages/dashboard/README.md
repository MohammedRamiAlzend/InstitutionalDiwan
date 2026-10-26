# `src/pages/dashboard` — Diwan Executive Dashboard Page Slice (`/`)
## 1. Purpose
Renders the main landing dashboard at `/` after authentication, displaying summary metrics for incoming/outgoing/internal documents, recent correspondence activity, and quick-action shortcuts.

The recent-activity list uses the shared data grid in compact mode (client-side paging, a column
chooser persisted under `dashboard-recent`, and row context menus that link to the record).
## 2. Strict FSD Rules
- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `DashboardPage` only via `src/pages/dashboard/index.ts`.
## 3. Planned Contents & Segments
- `ui/DashboardPage.tsx` — Composes KPI cards, quick document creation trigger, and recent registry items.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `lucide-react`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `DashboardPage`
## 6. Change Log
- 2026-10-25: Documented the compact shared data grid and row context menus for recent activity.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/dashboard` slice folder and `README.md`.
