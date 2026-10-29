# institutionaldiwan.front / src/pages — Route Pages Layer

> **Status:** Module 01 Organization has department management/hierarchy and institution-settings
> pages. Module 02 Identity pages are implemented, but auth/admin audit fixes and final verification
> are in progress. Protected pages except `/change-password` render inside `widgets/app-shell`.
> Module 03 Correspondence has a live registry page (`/documents`, Phase 2) with an external-
> organizations admin tab (Phase 7), a live details page (`/documents/:documentId`, Phases 3–6), a
> live numbering page (`/numbering`, Phase 5), and a live movements page (`/movements`, Phase 6).
> Module 09 Audit has a live page (`/audit`: log search + integrity reports, read-only for view
> roles, checkpoint/check for `AUDIT_MANAGE`).
> Other backend-ready modules remain pending; archive is blocked on Module 07.

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

## Contents — Slices & Backend Endpoint Mapping

| Slice Folder | Route(s) | Module & Live Backend Endpoints Used | State |
| --- | --- | --- | --- |
| `login/` | `/login` | 02 — `POST /api/auth/login` | ✅ Implemented |
| `change-password/` | `/change-password` | 02 — `POST /api/auth/change-password` | ✅ Implemented (forced after reset + voluntary) |
| `dashboard/` | `/` | Recent `GET /api/documents` + inbox summary `GET /api/movements/inbox` | ⚠️ Shell-owned content landing implemented; KPI/activity grid pending |
| `documents/` | `/documents`, `/documents/:documentId` | 03 + 06 — `GET/POST /api/documents` registry + create/draft, details/timeline, lifecycle commands, number history/lifecycle/department registration, movements/steps/events + routing commands, and the external-organizations tab (`GET/POST/PUT/DELETE /api/external-organizations`, `POST .../activate`); attachments panel in a later phase | Registry ✅ (Phase 2); details/participants/timeline/lifecycle ✅ (Phase 3); grants ✅ (Phase 4); numbers ✅ (Phase 5); movements tab ✅ (Phase 6); organizations tab ✅ (Phase 7) |
| `movements/` | `/movements` | 03 — inbox/outbox (`?tab=inbox\|outbox`) + routing actions | ✅ Implemented (Phase 6) |
| `departments/` | `/departments` | 01 — department reads/create/update/reparent/activate/deactivate; scoped employee reads | Implemented: tree/Mermaid/chart/grid, management, lazy employee branches and paged rosters. |
| `institution-settings/` | `/institution-settings` | 01 — `GET/PUT /api/institution-settings`, department/numbering choices and employee picker | Implemented behind `DEPARTMENT_MANAGE`; no automatic missing-settings creation. |
| `users/` | `/users` | 02 — users list/CRUD, role assignment, department assignments, effective permissions | ✅ Implemented |
| `roles/` | `/roles` | 02 — roles CRUD, activate/deactivate, permission matrix | ✅ Implemented |
| `employees/` | `/employees` | 02 — employees list/CRUD, activate/deactivate | ✅ Implemented |
| `numbering/` | `/numbering` | 04 — `GET/POST/PUT/DELETE /api/numbering/registries`, activate, `GET /api/departments/{id}/register` | ✅ Implemented (Phase 5) |
| `audit/` | `/audit` | 09 — `GET /api/audit-logs`, checkpoints, integrity checks/reports (`?panel=logs\|integrity`) | ✅ Implemented (Module 09) |
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
| `departments/` | Implemented: hierarchy views, department management, lazy employee branches and paged rosters. |
| `institution-settings/` | Implemented: guarded singleton settings form. |
| `documents/` | Registry ✅ (Phase 2); details ✅ (Phase 3: tabs, participants, timeline, lifecycle actions); grants ✅ (Phase 4); numbers ✅ (Phase 5: issue/cancel/replace/department registration/history); movements ✅ (Phase 6: open/routing actions, step timeline, event log); organizations ✅ (Phase 7: registry/organizations tabs, correspondent create/edit/deactivate/activate behind `DOCUMENT_EDIT`). |
| `numbering/` | ✅ Implemented (Phase 5: registry grid + department register book behind `NUMBERING_VIEW`, writes behind `NUMBERING_MANAGE`). |
| `movements/` | ✅ Implemented (Phase 6: inbox/outbox tabs behind `DOCUMENT_VIEW`, routing menus behind `DOCUMENT_EDIT`). |
| `audit/` | ✅ Implemented (Module 09: log/integrity panels behind `AUDIT_VIEW`, checkpoint and integrity-check actions behind `AUDIT_MANAGE`; browser-verified for both roles). |
| `archive/` | Blocked on Module 07 backend. |
| Final browser/build/test verification | ✅ Module 03 Phase 8 (2026-10-29): build, lint, backend build, and 487 tests pass; role-matrix browser pass (document / numbering / grants managers) plus draft, canceled, replaced-number, and movement states. |
| Module 02 audit items | Still open (tracked outside Module 03). |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Module 09: implemented `audit/` — `/audit` gated on `AUDIT_VIEW` with the log and integrity-report panels (`?panel=`), enabled the «سجل التدقيق» sidebar entry, and verified both the manager and view-only roles in the browser. |
| 2026-10-29 | agent | Module 03 Phase 8: recorded the final verification — role-matrix pages access (document/numbering/grants managers) with permission-filtered navigation, chips, and actions; no page code changed. |
| 2026-10-29 | agent | Module 03 Phase 7: added the external-organizations admin tab to `documents/` (`?tab=registry\|organizations`, page-level `DOCUMENT_VIEW`, writes behind `DOCUMENT_EDIT`) and verified create/duplicate/edit/deactivate/activate plus the participant-picker effect in-browser. No new route or nav entry. |
| 2026-10-29 | agent | Module 03 Phase 6: implemented `movements/` (`/movements`: inbox/outbox tabs via URL, client filters/sort/paging, routing menus) and the document-details movements tab (`DocumentMovementsTab`: open movement, status/permission-filtered actions, step timeline, Arabic event log); enabled the «الحركات» nav item and verified the full cycle in-browser. |
| 2026-10-29 | agent | Module 03 Phase 5: implemented `numbering/` (`/numbering`: registry grid + department register book, `NUMBERING_VIEW` gate) and the document-details numbers tab (`DocumentNumbersTab`, issue/cancel/replace/department registration/history behind `NUMBERING_VIEW`/`NUMBERING_MANAGE`); enabled the «الترقيم» nav item and verified both in-browser. |
| 2026-10-28 | agent | Module 03 Phase 3: implemented `documents/DocumentDetailsPage` — `/documents/:documentId` with URL tabs, status/permission-gated lifecycle actions, participants, status history, and the structural Arabic timeline. |
| 2026-10-28 | agent | Module 03 Phase 2: implemented `documents/` registry — `/documents` gates on `DOCUMENT_VIEW`, mounts `widgets/document-registry-table` and `features/document-create`; the detail route stays Phase 3 so no dead route is registered. |
| 2026-10-28 | agent | Update Module 01 page coverage for department writes, employee exploration, chart, and the new institution-settings route; distinguish Module 03 plans from executable pages. |
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
