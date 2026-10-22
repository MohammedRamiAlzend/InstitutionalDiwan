# `src/widgets/app-shell` — RTL Application Shell, Sidebar & Header Widget
## 1. Purpose
Composes the main authenticated Arabic RTL application layout, including the collapsible navigation sidebar (filtered by user permissions), top header bar, active department badge, and logout trigger.
## 2. Strict FSD Rules
- Allowed Imports: `src/features/auth`, `src/entities/user`, `src/entities/department`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, or sibling `src/widgets/*` slices.
- Public API: Export `AppShell`, `AppSidebar`, and `AppHeader` only via `src/widgets/app-shell/index.ts`.
## 3. Planned Contents & Segments
- `ui/AppShell.tsx` — Root `<div dir="rtl">` layout wrapper with `<Outlet />`.
- `ui/AppSidebar.tsx` — Permission-aware RTL navigation menu using `lucide-react` icons.
- `ui/AppHeader.tsx` — User profile summary, department badge, and `LogoutButton` from `features/auth`.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `react-router-dom`, `lucide-react`, `clsx`, `tailwind-merge`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `AppShell`, `AppSidebar`, and `AppHeader`
## 6. Change Log
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `widgets/app-shell` slice folder and `README.md`.
