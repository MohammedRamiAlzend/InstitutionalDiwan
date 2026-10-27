# `src/pages/dashboard` — Diwan Executive Dashboard Page Slice (`/`)
## 1. Purpose
Renders the authenticated landing page at `/`. For Identity Module 01 it is a minimal greeting screen (username, session permission count, logout, theme toggle); the KPI cards, quick actions, and recent-activity grid arrive with the Correspondence module.

The planned recent-activity list uses the shared data grid in compact mode (client-side paging, a column
chooser persisted under `dashboard-recent`, and row context menus that link to the record).
## 2. Strict FSD Rules
- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `DashboardPage` only via `src/pages/dashboard/index.ts`.
## 3. Contents & Segments
- `ui/DashboardPage.tsx` — Minimal authenticated landing (header with theme toggle + logout, greeting, permission count). KPI cards and recent registry items are pending.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `lucide-react` — ✅ installed
## 5. Implementation Status
- ✅ Minimal authenticated landing implemented (Identity Module 01)
- ❌ KPI cards, quick document creation, and the recent-activity data grid pending
## 6. Change Log
- 2026-10-27: Implemented the minimal landing so the login flow has a protected destination; marked the KPI/activity sections as pending.
- 2026-10-25: Documented the compact shared data grid and row context menus for recent activity.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/dashboard` slice folder and `README.md`.
