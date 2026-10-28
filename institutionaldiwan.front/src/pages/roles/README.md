# `src/pages/roles` — Roles & Permissions Administration Page Slice (`/roles`)

> **Status:** Implemented (Module 02 Phase 4, 2026-10-28) — the `/roles` screen gates on `ROLE_MANAGE` and mounts the roles administration grid.

## 1. Purpose

Renders the roles administration route. The page is a thin composition unit: it owns the
route-level permission gate and the Arabic page header, and delegates the table, dialogs, and row
actions to `widgets/roles-table`.

## 2. Strict FSD Rules

- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `RolesPage` only via `src/pages/roles/index.ts`.

## 3. Contents & Segments

- `ui/RolesPage.tsx` — `RequirePermission(ROLE_MANAGE)` wrapper, page header («الأدوار والصلاحيات»),
  and `<RolesTable />`.
- `index.ts` — Public API barrel file.

## 4. Required Packages

- `lucide-react` (via the page header and the widget)

## 5. Implementation Status

- ✅ `/roles` route mounted inside the app shell and gated on `ROLE_MANAGE` (Arabic forbidden state otherwise).
- ✅ Roles grid, create/edit dialog, and permission matrix verified in-browser.
- ✅ Employee assignment to users stays on `/users`; employee records themselves live on `/employees` (Phase 5).

## 6. Change Log

- 2026-10-28: Noted after Phase 5 that employee records live on `/employees`; this page owns roles and their permission matrix only.
- 2026-10-28: Implemented Phase 4 — the `/roles` page mounts `widgets/roles-table` behind the
  `ROLE_MANAGE` guard; created this slice README.
