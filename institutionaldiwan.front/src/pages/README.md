# institutionaldiwan.front / src/pages — Route Pages Layer

> **Status:** Partially implemented — `login/`, `change-password/`, the `dashboard/` landing, the
> `/users`, `/roles`, and `/employees` administration pages, and the `/departments` hierarchy page are
> live; every protected page except `/change-password` renders inside `widgets/app-shell`. The
> remaining slices are documented and pending.
> Backend-ready pages include Modules 01–06 and 09; archive stays blocked on Module 07.

## Purpose

`src/pages/` contains one slice per application route family in the Institutional Diwan SPA. Pages
are thin composition units: they read route/search params, arrange `widgets`, `features`, and
`entities` into a layout, and handle page-level loading/error boundaries. Pages must never contain
complex business mutations, raw `axios`/`fetch` calls, or reusable domain models.

## Layer & Import Rules (FSD Strict Hierarchy)

`pages` is the second layer from the top (`app → pages → widgets → features → entities → shared`):

- Allowed imports: `@/widgets/*`, `@/features/*`, `@/entities/*`, `@/shared/*` (via public barrels).
- Forbidden: `@/app/*` or sibling `@/pages/*` slices.
- Public API: each `pages/<slice>/` exposes route page component(s) only through `index.ts`.

## Required Packages

| Package | Role |
| --- | --- |
| `react-router-dom` | `useParams`, `useSearchParams`, `useNavigate`, `<Link>`, `<Outlet>` |
| `lucide-react` | Page header icons and empty-state illustrations |

## Planned Slices & Backend Endpoint Mapping

| Slice Folder | Route(s) | Module & Live Backend Endpoints Used | State |
| --- | --- | --- | --- |
| `login/` | `/login` | 02 — `POST /api/auth/login` | ✅ Implemented |
| `change-password/` | `/change-password` | 02 — `POST /api/auth/change-password` | ✅ Implemented (forced after reset + voluntary) |
| `dashboard/` | `/` | Recent `GET /api/documents` + inbox summary `GET /api/movements/inbox` | ⚠️ Shell-owned content landing implemented; KPI/activity grid pending |
| `documents/` | `/documents`, `/documents/:documentId` | 03 + 06 — documents CRUD/details + `GET/POST /api/documents/{id}/files` attachments panel | ❌ Planned |
| `movements/` | `/movements` | 05 — inbox/outbox (`?tab=inbox\|outbox`) + document routing actions | ❌ Planned |
| `departments/` | `/departments` | 01 — `GET /api/departments/tree` hierarchy (tree / Mermaid / table reads) | ✅ Implemented (read-only) |
| `users/` | `/users` | 02 — users list/CRUD, role assignment, department assignments, effective permissions | ✅ Implemented |
| `roles/` | `/roles` | 02 — roles CRUD, activate/deactivate, permission matrix | ✅ Implemented |
| `employees/` | `/employees` | 02 — employees list/CRUD, activate/deactivate | ✅ Implemented |
| `numbering/` | `/numbering` | 04 — numbering registries | ❌ Planned |
| `audit/` | `/audit` | 09 — `GET /api/audit-logs`, checkpoints, integrity checks/reports | ❌ Planned |
| `archive/` | `/archive` | 07 — **deferred** until Archiving API exists | ❌ Blocked |

## Screen acceptance (pages)

Every page must compose loading / empty / error / forbidden states (see root `AGENTS.md`). Filters,
search, sorting, pagination, and tabs belong in URL search params.

Data pages mount `widgets/*-table` / `widgets/*-inbox` (the shared data grid) rather than rendering
tables directly, so the column chooser, pagination, and row/header context menus are identical
everywhere. Pages with hierarchy or diagram content mount the owning widget (for example
`widgets/department-hierarchy-tree`), which composes `shared/ui/mermaid-view` for the diagram view;
the cross-module `widgets/visualization-panel` remains planned.

## Implementation status

| Slice | State |
| --- | --- |
| `login/`, `change-password/`, `dashboard/` | ✅ Implemented (dashboard renders inside the app shell; change-password is standalone) |
| `users/`, `roles/`, `employees/` | ✅ Implemented (`/users`, `/roles`, `/employees`) |
| `departments/` | ✅ Implemented read-only (tree / Mermaid / table); manage actions pending |
| `documents/`, `numbering/` | ❌ Not implemented (backend APIs live) |
| `movements/`, `audit/` | ❌ Not implemented (backend live; slice folders documented) |
| `archive/` | ❌ Blocked on Module 07 backend |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Module 01 Phase 1: implemented `departments/` (read-only) — the `/departments` page gates on `DEPARTMENT_MANAGE` and mounts `widgets/department-hierarchy-tree` (tree / Mermaid / table, search, detail dialog). |
| 2026-10-28 | agent | Phase 6: implemented `change-password/` — the standalone `/change-password` page mounts `features/auth`'s form, shows the forced-change notice, and redirects to the dashboard after a fresh token is applied. |
| 2026-10-28 | agent | Phase 5: implemented `employees/` — the `/employees` page gates on `EMPLOYEE_MANAGE` and mounts `widgets/employees-table`. |
| 2026-10-28 | agent | Phase 4: implemented `roles/` — the `/roles` page gates on `ROLE_MANAGE` and mounts `widgets/roles-table`; employees stay on their own future route. |
| 2026-10-28 | agent | Phase 3: implemented `users/` — the `/users` page gates on `USER_MANAGE` and mounts `widgets/users-table`; role and employee administration move to their own routes. |
| 2026-10-27 | agent | Phase 1: `dashboard/` is now shell-owned content and all protected pages render inside `widgets/app-shell`; pages no longer own header/theme/logout chrome. |
| 2026-10-27 | agent | Marked `login/` (login only) and the minimal `dashboard/` as implemented for Identity Module 01; `/change-password` and KPI/activity content remain pending. |
| 2026-10-25 | Buffy | Documented that pages compose the shared data grid and visualization widgets, so table preferences and context menus stay consistent across routes. |
| 2026-10-25 | Cursor | Aligned route map with live Modules 05/06/09: movements + audit as first-class pages; documents host attachments; archive marked blocked. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/pages` FSD layer folder and documented route slices, backend mappings, and FSD rules. |
