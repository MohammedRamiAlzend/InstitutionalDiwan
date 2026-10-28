# `src/pages/users` — Users Administration Page Slice (`/users`)

> **Status:** Implemented (Module 02 Phase 3, 2026-10-28) — the `/users` screen gates on `USER_MANAGE` and mounts the users administration grid. Roles and permissions administration lives on the separate `/roles` page (Phase 4); employees administration lives on `/employees` (Phase 5).

## 1. Purpose

Renders the users administration route. The page is a thin composition unit: it owns the route-level permission gate and the Arabic page header, and delegates the table, dialogs, and row actions to `widgets/users-table`.

## 2. Strict FSD Rules

- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `UsersPage` only via `src/pages/users/index.ts`.

## 3. Contents & Segments

- `ui/UsersPage.tsx` — `RequirePermission(USER_MANAGE)` wrapper, page header («إدارة المستخدمين»), and `<UsersTable />`.
- `index.ts` — Public API barrel file.

## 4. Required Packages

- `lucide-react` (via the page header and the widget)

## 5. Implementation Status

- ✅ `/users` route mounted inside the app shell and gated on `USER_MANAGE` (Arabic forbidden state otherwise).
- ✅ Users grid with create/edit/roles/access dialogs verified in-browser.
- ➡️ Roles & permissions matrix — live on `/roles` (Phase 4), not on this page.
- ➡️ Employees administration — live on `/employees` (Phase 5), not on this page.

## 6. Change Log

- 2026-10-28: Documented the boundary after Phase 5 — roles and employees administration live on their own routes (`/roles`, `/employees`); this page stays focused on user accounts and their assignments.
- 2026-10-28: Implemented Phase 3 — the `/users` page mounts `widgets/users-table` behind the `USER_MANAGE` guard; role and employee administration deferred to their own routes.
- 2026-10-25: Documented the shared data-grid and context-menu requirements for the admin lists.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/users` slice folder and `README.md`.
