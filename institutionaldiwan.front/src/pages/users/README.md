# `src/pages/users` — Users & Roles Administration Page Slice (`/users`)
## 1. Purpose
Renders the user and role administration screen at `/users`, allowing administrators to manage institutional accounts (`features/user-manage`) and configure the permission matrix (`features/role-manage`).

Every list (users, roles, permissions, employees, assignments) uses the shared data grid: filters,
search, sorting, and pagination in the URL, a column chooser persisted per table id in
`localStorage`, and permission-filtered row/header context menus (activate, deactivate, assign role,
reset access, copy id).
## 2. Strict FSD Rules
- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `UsersPage` only via `src/pages/users/index.ts`.
## 3. Planned Contents & Segments
- `ui/UsersPage.tsx` — Tabbed administration interface for Users list and Roles & Permissions matrix.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-table`, `@tanstack/react-query`, `lucide-react`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `UsersPage`
## 6. Change Log
- 2026-10-25: Documented the shared data-grid and context-menu requirements for the admin lists.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/users` slice folder and `README.md`.
