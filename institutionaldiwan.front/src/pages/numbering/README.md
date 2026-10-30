# `src/pages/numbering` — Numbering Registry & Department Register Page (`/numbering`)

> **Status:** ✅ Implemented (Module 03 Phase 5; tabbed in Module 04 Phase 2; register book moved into a
> widget in Module 04 Phase 3) and verified in-browser.

## 1. Purpose

Renders the numbering administration page at `/numbering` as two tabs: the official numbering
registries (`widgets/numbering-registry-table`) and a read-only department register book
(`widgets/department-register-book`) that lists the numbers departments wrote in their own registers
next to the central Diwan number. The active tab is URL-backed (`?tab=register`, default
`registries`), so a tab is linkable and survives reloads.

The page gates on `NUMBERING_VIEW`. Registry writes (`NUMBERING_MANAGE`) are hidden for readers, and
the server rejects direct writes with 403.

## 2. Strict FSD Rules

- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `NumberingPage` only via `src/pages/numbering/index.ts`.

## 3. Contents

| File | Purpose |
| --- | --- |
| `ui/NumberingPage.tsx` | `RequirePermission NUMBERING_VIEW` + «الترقيم» header and two URL-backed tabs (`role="tablist"`): «سجلات الترقيم» (`widgets/numbering-registry-table`) and «سجل الدوائر» (`widgets/department-register-book`). |
| `index.ts` | Public API barrel. |

## 4. Required Packages

- `@tanstack/react-table`, `@tanstack/react-query`, `react-router-dom`, `lucide-react`

## 5. Implementation Status

- ✅ `NumberingPage` with `NUMBERING_VIEW` gate
- ✅ Two URL-backed tabs (registries / department register)
- ✅ Registry grid with `NUMBERING_MANAGE`-filtered create/edit/activate actions
- ✅ Department register book (read-only, `GET /api/departments/{id}/register`) rendered by
  `widgets/department-register-book` on the shared data grid
- ✅ Verified in-browser: empty state, registry CRUD/status, counters, department register empty and
  populated states, denial for a `USER`-role caller, tab deep-links, registry filters with URL
  persistence, duplicate-scope warnings, next-number preview, template-token validation, and a
  department register book with URL-backed department/year, sorting, search, and menus

## 6. Change Log

- 2026-10-30: Module 04 Phase 5 — verified the page in-browser as manager and read-only user: the
  manager flow (create/deactivate/activate, URL filters, duplicate banner, template lock, next-number
  preview) and a `NUM-VIEW` user (no create button, view/copy row menus only). The department register
  now has a permanent inactive fixture (`VER-04-P5`) that keeps its historical line.
- 2026-10-29: Module 04 Phase 3 — removed the bespoke `DepartmentRegisterSection` and mounted
  `widgets/department-register-book` instead; the register grid now has URL state, sorting, search,
  column chooser, and context menus.
- 2026-10-29: Module 04 Phase 2 — split the page into URL-backed tabs and documented the tab state.
- 2026-10-29: Module 03 Phase 5 — implemented the page, the department register section, and verified
  the whole numbering surface in-browser.
- 2026-10-25: Documented the shared data-grid and context-menu requirements for the counters table.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `pages/numbering` slice folder and `README.md`.
