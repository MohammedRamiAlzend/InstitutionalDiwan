# institutionaldiwan.front / src — Front-End Source Root

> **Status:** Identity Module 01 and the Module 02 Identity admin front end (Phases 0–6,
> 2026-10-28) implemented, plus Module 01 Phase 1 — the organizational hierarchy page (read-only):
> the `app` layer boots the SPA and forces password resets, `shared`
> provides the HTTP client, tokens, UI kit, and the lazy Mermaid renderer, `entities/user` holds the
> session and user admin reads, `entities/{department,role,employee,lookup}` provide the supporting
> admin reads (with `department/model/toMermaid`), `features/{auth,user-manage,role-manage,employee-manage}`
> own login/logout/password changes, user writes (including the administrator password reset),
> role/permission writes, and employee writes, `widgets/{app-shell,users-table,roles-table,employees-table,department-hierarchy-tree}`
> wrap every shell route, render the admin grids, and draw the hierarchy as a tree / Mermaid / table
> (with the department's employees listed in its detail dialog),
> and `pages/{login,change-password,dashboard,departments,users,roles,employees}` are
> live routes (`/change-password` is standalone). Remaining module pages are documented but not yet coded.

## Purpose

The root of the front end's source tree. It holds the Vite entry point that boots the React
application, plus the six Feature-Sliced Design layers the SPA is built from.

## Contents

| File / Folder | Purpose |
| --- | --- |
| `main.tsx` | Vite entry point: imports `app/styles/index.css` and mounts the `app` layer root. |
| `app/` | FSD layer 1 — global providers, router and permission guards, theme tokens and global styles. |
| `pages/` | FSD layer 2 — one route page per slice (`login`, `change-password`, `dashboard`, `departments`, `users`, `roles`, `employees` implemented). |
| `widgets/` | FSD layer 3 — composite page sections (`app-shell`, `users-table`, `roles-table`, `employees-table`, `department-hierarchy-tree` implemented; rest documented). |
| `features/` | FSD layer 4 — write mutations, Zod schemas, interactive forms (`auth`, `user-manage`, `role-manage`, `employee-manage` implemented). |
| `entities/` | FSD layer 5 — read models, query key factories, `useQuery` hooks (`user`, `department`, `role`, `employee`, `lookup` implemented). |
| `shared/` | FSD layer 6 — HTTP client, RFC 7807 → Arabic message catalog, RTL UI kit (data grid, menus, dialogs, Mermaid renderer), table-state utilities, constants. |
| `assets/` | Vite template images (`hero.png`, `react.svg`, `vite.svg`); replaced by the institution's own artwork. |

## Conventions / Notes

- Imports flow strictly downward: `app` → `pages` → `widgets` → `features` → `entities` →
  `shared`. Never import upward and never import across sibling slices on the same layer.
- Every slice and `shared` segment exposes its public interface through its own `index.ts`
  barrel; deep imports are forbidden.
- The `@` path alias points at this folder (`./src`), configured in `vite.config.ts` and
  `tsconfig.app.json`.
- Each layer folder has its own `README.md` describing its rules, its planned slices, and its
  change log.

## Implementation status

| Item | State |
| --- | --- |
| `main.tsx` | ✅ Mounts `@/app` + global stylesheet; template `App.tsx` / `App.css` / `index.css` removed |
| `app/` | ✅ Implemented |
| `pages/` | ✅ `login`, `change-password` (standalone), `dashboard` (inside the app shell), `departments`, `users`, `roles`, `employees`; remaining slices pending |
| `widgets/` | ✅ `app-shell`, `users-table`, `roles-table`, `employees-table`, `department-hierarchy-tree`; remaining widgets pending |
| `features/` | ✅ `auth`, `user-manage`, `role-manage`, `employee-manage`; remaining slices pending |
| `entities/` | ✅ `user`, `department`, `role`, `employee`, `lookup`; remaining slices pending |
| `shared/` | ✅ Core (`api`, `config`, `lib`, `types`, `ui` including the shared data grid, menu primitives, and `mermaid-view`) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Clicking a department now lists its employees: `entities/employee` gained `departmentId`/`includeSubDepartments` list params and the hierarchy detail dialog mounts `DepartmentEmployeesSection` (count, sub-departments toggle, permission gate, loading/error/empty). Build and lint pass. |
| 2026-10-28 | agent | Implemented Module 01 Phase 1: `shared/ui/mermaid-view` (lazy, strict), `entities/department/model/toMermaid`, `widgets/department-hierarchy-tree` (tree / Mermaid / table, URL-driven search and view, node menus, detail dialog), and the `/departments` page + route + sidebar entry behind `DEPARTMENT_MANAGE`. Verified in-browser; build and lint pass. |
| 2026-10-28 | agent | Added the administrator password reset: `ResetPasswordDialog` + `useResetUserPasswordMutation` in `features/user-manage`, the «إعادة تعيين كلمة المرور» row action in `widgets/users-table`, the shared `shared/ui` `PasswordField`, and the `POST /api/users/{userId}/reset-password` contract (forces a password change at the next sign-in). Build and lint pass. |
| 2026-10-28 | agent | Completed Module 02 Phase 6: implemented `/change-password` (`features/auth` schema/mutation/form + `pages/change-password`), forced the redirect from the JWT claim so it survives reloads, routed reset logins there, added the account-menu entry and `Identity.NewPasswordSameAsOld`, and verified the full Identity admin flow end to end. Build and lint pass. |
| 2026-10-28 | agent | Implemented Module 02 Phase 5: `features/employee-manage` (create/edit form, activate/deactivate, department selector), `widgets/employees-table` (server-side search and paging, row actions, status), the `/employees` page behind `EMPLOYEE_MANAGE`, and the two employee error codes in the Arabic catalog; enabled its sidebar entry. Verified in-browser; build and lint pass. |
| 2026-10-28 | agent | Implemented Module 02 Phase 4: `features/role-manage` (create/edit, activate/deactivate, permission matrix with staged diff), `widgets/roles-table`, the `/roles` page behind `ROLE_MANAGE`, and the shared permission-module labels in `entities/role`; enabled its sidebar entry. Verified in-browser; build and lint pass. |
| 2026-10-28 | agent | Implemented Module 02 Phase 3: `features/user-manage` (create/edit, status, role assignment, department assignments), `widgets/users-table`, and the `/users` page behind `USER_MANAGE`; enabled its sidebar entry. Verified in-browser; build and lint pass. |
| 2026-10-27 | agent | Implemented Module 02 Phase 2 supporting reads: `entities/{department,role,employee,lookup}` plus user list/detail/effective-permission hooks; deterministic lookup ids moved to `shared/config`; all read shapes verified against the live API. |
| 2026-10-27 | agent | Implemented the `widgets/app-shell` layer for Module 02 Phase 1 (sidebar, header, mobile drawer, settings dialog, shell/item context menus) and nested the dashboard inside it. |
| 2026-10-27 | agent | Expanded `shared` for Module 02: the data grid, context/dropdown menus, dialogs, select/checkbox, table-state and column-preference utilities, and the extended Arabic error catalog. |
| 2026-10-27 | agent | Rewired `main.tsx` to the `app` layer; deleted the Vite template `App.tsx`, `App.css`, and `index.css` now that `app/styles` owns the global stylesheet. |
| 2026-10-26 | agent | Ported this README into the standalone project; renamed the source root, noted the external API, and marked the `@` alias as not configured yet. |
| 2026-10-25 | Cursor | Noted docs are current for Modules 01–06 + 09 while application code remains the Vite template only. |
| 2026-10-22 | opencode | Added the missing `src/README.md`: the entry files the Vite template left behind, the six FSD layer folders, the import-direction rule, and the `@` path alias. |
