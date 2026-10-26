# `src/widgets/app-shell` — RTL Application Shell, Sidebar & Header Widget
## 1. Purpose
Composes the main authenticated Arabic RTL application layout, including the collapsible navigation sidebar (filtered by user permissions), top header bar, active department badge, and logout trigger.

The shell owns the desktop-style chrome: a right-click menu on empty chrome (refresh the active view, toggle table density, open settings) and context menus on sidebar entries (open, copy link, collapse group). All shell actions are also keyboard reachable through the shared context-menu primitive.

The header also hosts the **theme toggle** (light / dark / system), persisted as `diwan.theme` through the app `ThemeProvider`. The shell must render correctly in Arabic RTL and LTR, in both themes, using only `styles/tokens.css` variables.
## 2. Strict FSD Rules
- Allowed Imports: `src/features/auth`, `src/entities/user`, `src/entities/department`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, or sibling `src/widgets/*` slices.
- Public API: Export `AppShell`, `AppSidebar`, and `AppHeader` only via `src/widgets/app-shell/index.ts`.
## 3. Planned Contents & Segments
- `ui/AppShell.tsx` — Root `<div dir="rtl">` layout wrapper with `<Outlet />`.
- `ui/AppSidebar.tsx` — Permission-aware RTL navigation menu using `lucide-react` icons.
- `ui/AppHeader.tsx` — User profile summary, department badge, theme toggle, and `LogoutButton` from `features/auth`.
- `ui/ThemeToggle.tsx` — Light / dark / system switch backed by the app `ThemeProvider` (`diwan.theme`).
- `model/shell-menu.ts` — Permission-filtered menu item groups for empty chrome and sidebar entries.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `react-router-dom`, `lucide-react`, `clsx`, `tailwind-merge`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `AppShell`, `AppSidebar`, and `AppHeader`
## 6. Change Log
- 2026-10-25: Added the header theme toggle and documented RTL/LTR + light/dark expectations for the shell.
- 2026-10-25: Documented the app-shell right-click menus (empty chrome, sidebar) required by the interaction contract.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `widgets/app-shell` slice folder and `README.md`.
