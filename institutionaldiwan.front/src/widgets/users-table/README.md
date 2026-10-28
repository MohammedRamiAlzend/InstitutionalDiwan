# `src/widgets/users-table` — Users Administration Grid Widget

> **Status:** Implemented (Module 02 Phase 3, 2026-10-28) — paged/searchable user grid with permission-filtered row actions and the create/edit/roles/access/reset-password dialogs.

## 1. Purpose

Composes the user reads (`entities/user`) with the user writes (`features/user-manage`) into the `/users` administration surface: the shared data grid, a debounced server search, paging, a column chooser, and row/header/empty context menus whose items are permission-filtered.

## 2. Strict FSD Rules

- Allowed Imports: `@/features/user-manage`, `@/entities/{user,department,role}`, and `@/shared/*`.
- Forbidden Imports: NEVER import from `@/app`, `@/pages`, or sibling `@/widgets/*` slices.
- Public API: export `UsersTable` via `src/widgets/users-table/index.ts`.

## 3. Contents & Segments

- `ui/UsersTable.tsx`
  - `useUsers` paged query driven by `useTableState` (`page`, `pageSize`, `search` in the URL).
  - Columns: username (marked «أنت» for the signed-in account), primary department, scope, roles, status, and optional hidden columns (must-change-password, linked employee, created/updated timestamps).
  - Department names resolve through `useDepartments` (enabled by `DEPARTMENT_MANAGE`); unknown or stale department ids fall back to a shortened GUID with the full value in `title`. Role codes resolve to Arabic names through `useRoles` (enabled by `ROLE_MANAGE`), falling back to the code.
  - Row menu: edit, manage roles (disabled without `ROLE_MANAGE`), permissions & assignments, reset password (disabled for the current account), activate/deactivate (disabled for the current account), copy username, copy id.
  - Header/empty menus: create user and refresh. Rows and the grid surface also open the same menu on right-click / `Shift+F10`.
  - Status changes go through `ConfirmDialog` with an Arabic description; success and failure surface as toasts.
- `index.ts` — Public API barrel file.

## 4. Required Packages

- `@tanstack/react-query`, `lucide-react`, `sonner`

## 5. Implementation Status

- ✅ Grid, server search, paging, column chooser, and status confirmation implemented.
- ✅ Create/edit/roles/access dialogs mounted from the row menu; self-deactivation blocked.
- ✅ «إعادة تعيين كلمة المرور» opens the admin reset dialog (blocked for the signed-in account).
- ✅ Verified in-browser against the live API (RTL, light/dark, search, dialogs, Arabic validation, reset flow with a forced change on next login).
- ❌ No server-side sorting yet — the API exposes `page`/`pageSize`/`search` only for users.

## 6. Change Log

- 2026-10-28: Added the «إعادة تعيين كلمة المرور» row action (disabled for self) that mounts `ResetPasswordDialog` from `features/user-manage`.
- 2026-10-28: Implemented Phase 3 — the users grid with permission-filtered actions, stale-department fallback, status confirmation, and the three feature dialogs; created this slice README.
- 2026-10-19: Documented the `users-table` widget as a planned slice in `src/widgets/README.md`.
