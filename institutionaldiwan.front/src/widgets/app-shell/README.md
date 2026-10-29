# `src/widgets/app-shell` — RTL Application Shell, Sidebar & Header Widget
> **Status:** Implemented (Phase 1, 2026-10-27) — wraps every shell route (the standalone `/change-password` page is outside it): permission-filtered sidebar, sticky header, mobile drawer, shell/user/item context menus, and the display-settings dialog (theme + density).
## 1. Purpose
Composes the authenticated Arabic RTL workspace chrome around `<Outlet />`: a collapsible navigation sidebar filtered by the session's effective permissions, a sticky top bar with shell actions, theme toggle, and account menu, a mobile overlay drawer, and the global right-click surface.

The shell owns the desktop-style chrome: a context menu on page content (refresh queries, toggle density, open display settings) and context menus on sidebar entries (open, copy link, collapse/expand group). All shell actions are also keyboard reachable through the shared context-menu primitive.
## 2. Strict FSD Rules
- Allowed Imports: `src/features/auth`, `src/entities/user`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, or sibling `src/widgets/*` slices.
- Public API: exports `AppShell`, `AppSidebar`, and `AppHeader` via `src/widgets/app-shell/index.ts`; `AppRouter` mounts only `<AppShell />`.
- Logout is not reimplemented here: the account menu calls `useLogout` from `features/auth`, which owns token/session/query-cache teardown.
## 3. Contents & Segments
- `ui/AppShell.tsx` — Layout wrapper: desktop sticky sidebar, mobile overlay drawer (Escape + body scroll lock), `AppHeader`, the page `<main>` wrapped in a shell `ContextMenu` (keyboard `Shift+F10` / `ContextMenu` dispatch), `SettingsDialog`, and the density effect that applies `html[data-density]` and persists `diwan.density` (attribute removed on unmount so `/login` keeps default density).
- `ui/AppSidebar.tsx` — Brand block, collapse toggle, permission-filtered nav groups (`filterNavGroups`), active `NavLink` styling, disabled entries with the Arabic «قريبًا» badge for slices that have not landed, per-item `ContextMenu` (فتح / نسخ الرابط / طي المجموعة) with clipboard success/error toasts, and `Shift+F10` support on links. Each entry renders as one inline row (18px icon + label, `flex items-center gap-3`); the `NavLink` itself keeps a static `className` and hands its active state to an inner `<span>` render prop, because the context-menu trigger clones the link through Radix `Slot`, which stringifies a function `className` and silently drops all styling.
- `ui/AppHeader.tsx` — Mobile menu trigger, current-route title, shell-actions `DropdownMenu`, `ThemeToggle`, and the account `DropdownMenu` (avatar initial, username, roles; تغيير كلمة المرور، إعدادات العرض، then danger تسجيل الخروج).
- `ui/SettingsDialog.tsx` — Display settings dialog: theme choice (فاتح / داكن / حسب النظام through the app `ThemeProvider`) and density choice (مريح / مضغوط) with `aria-pressed` state.
- `model/shell-nav.ts` — `SHELL_NAV_GROUPS` (نظرة عامة، المراسلات، التنظيم، إدارة الوصول، الرقابة والتدقيق), permission codes, and `available` flags. Dashboard, documents, movements, numbering, departments, institution settings, users, roles, employees, and audit are available; remaining entries show «قريبًا». Both organization entries require `DEPARTMENT_MANAGE`. `filterNavGroups` hides unpermitted items/empty groups; `findNavItem` drives route titles.
- `model/density.ts` — `Density`, `getStoredDensity`, `applyDensity` (`html[data-density]`), `persistDensity` (`diwan.density`).
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `react-router-dom`, `lucide-react`, `sonner`, `clsx`, `tailwind-merge` — ✅ installed
## 5. Implementation status
| Item | State |
| --- | --- |
| Shell/sidebar/header, display settings, and menus | Implemented; final regression verification pending. |
| Shell route children | Dashboard, documents, numbering, departments, institution settings, users, roles, and employees; `/change-password` stays standalone. |
| Institution settings navigation | Available under organization for `DEPARTMENT_MANAGE`; distinct from `SettingsDialog` (local theme/density only). |
| Module 03 document navigation | «الوثائق» available under المراسلات for `DOCUMENT_VIEW` (Phase 2); «الحركات» available for `DOCUMENT_VIEW` (Phase 6); «الترقيم» available for `NUMBERING_VIEW` (Phase 5). |
| Module 09 audit navigation | «سجل التدقيق» available under الرقابة والتدقيق for `AUDIT_VIEW`; verified for both a manager and a view-only role. |
| Department badge / SignalR bell | Pending their slices. |
## 6. Change Log
- 2026-10-29: Enabled the «سجل التدقيق» audit navigation entry now that the `/audit` page is live (Module 09), gated on `AUDIT_VIEW`.
- 2026-10-29: Enabled the «الحركات» correspondence navigation entry now that the `/movements` inbox/outbox page is live (Module 03 Phase 6); every correspondence entry now links to a real page.
- 2026-10-29: Enabled the «الترقيم» correspondence navigation entry now that the `/numbering` page is live (Module 03 Phase 5).
- 2026-10-28: Enabled the «الوثائق» correspondence navigation entry now that the `/documents` registry is live (Module 03 Phase 2); movements and numbering remain «قريبًا».
- 2026-10-28: Document the enabled institution-settings organization entry, its permission gate, and separation from local display settings; final browser verification remains pending.
- 2026-10-28: Enabled the الأقسام التنظيمية navigation entry now that the `/departments` page is live (Module 01 Phase 1).
- 2026-10-28: Fixed the sidebar entry layout: the `NavLink` now uses a static `className` with an inner flex row (icon + label on one line) instead of a function `className`, which the context-menu trigger's Radix `Slot` stringified and dropped — that left every entry unstyled and stacked the icon above its label. The nav icon is now 18px (`size-4.5`) instead of 20px. Verified in the mobile drawer (row centers align, active background, right-click menu, navigation).
- 2026-10-28: Added the account-menu «تغيير كلمة المرور» entry now that `/change-password` is live.
- 2026-10-28: Enabled the الموظفون navigation entry now that the `/employees` page is live (Phase 5).
- 2026-10-28: Enabled the الأدوار والصلاحيات navigation entry now that the `/roles` page is live (Phase 4).
- 2026-10-28: Enabled the المستخدمون navigation entry now that the `/users` page is live (Phase 3).
- 2026-10-27: Implemented Phase 1 — shell layout, permission-filtered collapsible sidebar with «قريبًا» states, header with shell/account menus, mobile drawer, display-settings dialog (theme + density), shell and item context menus with keyboard dispatch, and the shared `useLogout` wiring. Verified in-browser; build and lint pass.
- 2026-10-25: Added the header theme toggle and documented RTL/LTR + light/dark expectations for the shell.
- 2026-10-25: Documented the app-shell right-click menus (empty chrome, sidebar) required by the interaction contract.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `widgets/app-shell` slice folder and `README.md`.
