# institutionaldiwan.client / src/pages — Route Pages Layer
> **Status:** Scaffolding & Rules — FSD route slices and composition rules defined; code pending.
## Purpose
`src/pages/` contains one slice per application route in the Institutional Diwan SPA. Pages are thin composition units: they read route parameters/search params, arrange `widgets`, `features`, and `entities` into a page layout, and handle page-level loading/error boundaries. Pages must never contain complex business mutations, raw `axios`/`fetch` calls, or reusable domain models.
## Layer & Import Rules (FSD Strict Hierarchy)
`pages` is the second layer from the top (app → pages → widgets → features → entities → shared):
- Allowed imports: `@/widgets/*`, `@/features/*`, `@/entities/*`, `@/shared/*` (always via each slice's public `index.ts` barrel).
- Forbidden imports: Cannot import from `@/app/*` or from another slice inside `@/pages/*` (no cross-page imports).
- Public API rule: Every page slice (`pages/<slice>/`) must expose only its route page component(s) through `pages/<slice>/index.ts`. Deep imports like `@/pages/documents/ui/DocumentsPage` are forbidden.
## Required Packages
| Package | Version / Role | Used in `src/pages/` for |
| --- | --- | --- |
| `react-router-dom` (or `react-router` v7) | `^7.x` | `useParams`, `useSearchParams`, `useNavigate`, `<Link>`, `<Outlet>` |
| `lucide-react` | `^1.x` | Page header icons and empty-state illustrations |
## Planned Slices & Backend Endpoint Mapping
| Slice Folder | Route(s) | Module & Live Backend Endpoints Used | State |
| --- | --- | --- | --- |
| `login/` | `/login`, `/change-password` | Module 02 — `POST /api/auth/login`, `POST /api/auth/change-password` | ❌ Planned |
| `dashboard/` | `/` | Overview metrics, recent documents (`GET /api/documents`), department summary | ❌ Planned |
| `documents/` | `/documents`, `/documents/:documentId` | Module 03 & 02 — `GET /api/documents`, `GET /api/documents/{id}`, `POST /api/documents`, `/api/documents/{id}/access-grants` | ❌ Planned |
| `departments/` | `/departments` | Module 01 — `GET /api/departments`, `GET /api/departments/tree`, `POST /api/departments`, `PUT /api/departments/{id}`, `PUT /api/departments/{id}/parent`, `DELETE`, `POST /activate` | ❌ Planned |
| `users/` | `/users`, `/roles`, `/employees` | Module 02 — `/api/users`, `/api/roles`, `/api/permissions`, `/api/employees` | ❌ Planned |
| `numbering/` | `/numbering` | Module 04 — `GET /api/numbering/registries`, `GET /api/numbering/registries/{id}` | ❌ Planned |
| `movements/` | `/movements` | Module 05 — Department inbox/outbox & Central Diwan routing (backend endpoints planned) | ❌ Planned |
| `archive/` | `/archive` | Module 07 — Advanced correspondence search & saved searches (backend endpoints planned) | ❌ Planned |
| `audit/` | `/audit` | Module 09 — Audit log viewer & SHA-256 chain integrity verification (backend endpoints planned) | ❌ Planned |
## Conventions / Vibe-Coding Notes (OpenCode & Freebuff)
- Keep pages under ~120 lines: If a page grows complex, extract composite sections into `@/widgets/*` and interactive forms/buttons into `@/features/*`.
- URL-driven filters: Store table filters, search queries, and active tabs in URL search params (`useSearchParams`) rather than local `useState` so correspondence views are bookmarkable and shareable.
- Golden Rule: Update this `README.md` and the specific slice's `README.md` in the same turn whenever files change.
## Implementation status
| Slice | State |
| --- | --- |
| `login/`, `dashboard/`, `documents/`, `departments/`, `users/`, `numbering/` | ❌ Not implemented (Backend APIs live & ready) |
| `movements/`, `archive/`, `audit/` | ❌ Not implemented (Awaiting backend slices) |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/pages` FSD layer folder and documented route slices, backend mappings, and FSD rules. |
