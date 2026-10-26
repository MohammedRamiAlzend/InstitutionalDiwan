# institutionaldiwan.front / src/pages — Route Pages Layer

> **Status:** Scaffolding & Rules — FSD route slices and composition rules defined; code pending.
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
| `login/` | `/login`, `/change-password` | 02 — `POST /api/auth/login`, `POST /api/auth/change-password` | ❌ Planned |
| `dashboard/` | `/` | Recent `GET /api/documents` + inbox summary `GET /api/movements/inbox` | ❌ Planned |
| `documents/` | `/documents`, `/documents/:documentId` | 03 + 06 — documents CRUD/details + `GET/POST /api/documents/{id}/files` attachments panel | ❌ Planned |
| `movements/` | `/movements` | 05 — inbox/outbox (`?tab=inbox\|outbox`) + document routing actions | ❌ Planned |
| `departments/` | `/departments` | 01 — departments tree/CRUD + institution settings entry | ❌ Planned |
| `users/` | `/users`, `/roles`, `/employees` | 02 — users, roles, permissions, employees | ❌ Planned |
| `numbering/` | `/numbering` | 04 — numbering registries | ❌ Planned |
| `audit/` | `/audit` | 09 — `GET /api/audit-logs`, checkpoints, integrity checks/reports | ❌ Planned |
| `archive/` | `/archive` | 07 — **deferred** until Archiving API exists | ❌ Blocked |

## Screen acceptance (pages)

Every page must compose loading / empty / error / forbidden states (see root `AGENTS.md`). Filters,
search, sorting, pagination, and tabs belong in URL search params.

Data pages mount `widgets/*-table` / `widgets/*-inbox` (the shared data grid) rather than rendering
tables directly, so the column chooser, pagination, and row/header context menus are identical
everywhere. Pages with hierarchy or diagram content mount `widgets/visualization-panel`, which
offers the Mermaid diagram / chart / tree switcher.

## Implementation status

| Slice | State |
| --- | --- |
| `login/`, `dashboard/`, `documents/`, `departments/`, `users/`, `numbering/` | ❌ Not implemented (backend APIs live) |
| `movements/`, `audit/` | ❌ Not implemented (backend live; slice folders documented) |
| `archive/` | ❌ Blocked on Module 07 backend |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Documented that pages compose the shared data grid and visualization widgets, so table preferences and context menus stay consistent across routes. |
| 2026-10-25 | Cursor | Aligned route map with live Modules 05/06/09: movements + audit as first-class pages; documents host attachments; archive marked blocked. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/pages` FSD layer folder and documented route slices, backend mappings, and FSD rules. |
