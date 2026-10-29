# `src/pages/numbering` — Numbering Registry & Department Register Page (`/numbering`)

> **Status:** ✅ Implemented (Module 03 Phase 5) and verified in-browser.

## 1. Purpose

Renders the numbering administration page at `/numbering`: the official numbering registries
(`widgets/numbering-registry-table`) and a read-only department register book
(`DepartmentRegisterSection`) that lists the numbers departments wrote in their own registers next to
the central Diwan number.

The page gates on `NUMBERING_VIEW`. Registry writes (`NUMBERING_MANAGE`) are hidden for readers, and
the server rejects direct writes with 403.

## 2. Strict FSD Rules

- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `NumberingPage` only via `src/pages/numbering/index.ts`.

## 3. Contents

| File | Purpose |
| --- | --- |
| `ui/NumberingPage.tsx` | `RequirePermission NUMBERING_VIEW` + «الترقيم» header, registry table, department register section. |
| `ui/DepartmentRegisterSection.tsx` | Department + numbering-year pickers (defaults to the current year), refresh, and a lines table showing the department register number, central Diwan number, registration date, status, and a link to the document; handles empty/loading/error states. |
| `index.ts` | Public API barrel. |

## 4. Required Packages

- `@tanstack/react-table`, `@tanstack/react-query`, `react-router-dom`, `lucide-react`

## 5. Implementation Status

- ✅ `NumberingPage` with `NUMBERING_VIEW` gate
- ✅ Registry grid with `NUMBERING_MANAGE`-filtered create/edit/activate actions
- ✅ Department register book (read-only, `GET /api/departments/{id}/register`)
- ✅ Verified in-browser: empty state, registry CRUD/status, counters, department register empty and
  populated states, and denial for a `USER`-role caller

## 6. Change Log

- 2026-10-29: Module 03 Phase 5 — implemented the page, the department register section, and verified
  the whole numbering surface in-browser.
- 2026-10-25: Documented the shared data-grid and context-menu requirements for the counters table.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/numbering` slice folder and `README.md`.
