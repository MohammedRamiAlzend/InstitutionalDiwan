# `src/pages/dashboard` — Diwan Executive Dashboard Page Slice (`/`)
## 1. Purpose
Renders the authenticated landing content at `/` inside the app shell. For Identity Module 02 Phase 1 it is a content-only welcome screen (page header, greeting, effective-permission count, roles, and a «ما التالي؟» note); the KPI cards, quick actions, and recent-activity grid arrive with the Correspondence module.

Because the shell owns navigation, theme, density, and logout, this page contains no chrome of its own.
## 2. Strict FSD Rules
- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `DashboardPage` only via `src/pages/dashboard/index.ts`.
## 3. Contents & Segments
- `ui/DashboardPage.tsx` — Content-only landing: `PageHeader`, welcome card, permission/role summary (`dir="ltr"` for role codes), and the next-steps note. KPI cards and the recent registry grid are pending.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `lucide-react` — ✅ installed (`@tanstack/react-query` arrives with the activity grid)
## 5. Implementation Status
- ✅ Content-only landing mounted as the first `AppShell` child (Phase 1)
- ❌ KPI cards, quick document creation, and the recent-activity data grid pending
## 6. Change Log
- 2026-10-27: Moved header/theme/logout chrome into `widgets/app-shell` (Phase 1) and rewrote the page as shell-owned content; role codes render as LTR islands.
- 2026-10-27: Implemented the minimal landing so the login flow has a protected destination; marked the KPI/activity sections as pending.
- 2026-10-25: Documented the compact shared data grid and row context menus for recent activity.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/dashboard` slice folder and `README.md`.
