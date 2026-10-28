# `src/widgets/roles-table` — Roles Administration Grid Widget

> **Status:** Implemented (Module 02 Phase 4, 2026-10-28) — client-side roles grid with permission-filtered row actions and the role/permissions dialogs.

## 1. Purpose

Composes the role reads (`entities/role`) with the role writes (`features/role-manage`) into the
`/roles` administration surface: the shared data grid, client-side search/sort/paging over the
plain-array `GET /api/roles` response, a column chooser, and row/header/empty context menus.

## 2. Strict FSD Rules

- Allowed Imports: `@/features/role-manage`, `@/entities/role`, and `@/shared/*`.
- Forbidden Imports: NEVER import from `@/app`, `@/pages`, or sibling `@/widgets/*` slices.
- Public API: export `RolesTable` via `src/widgets/roles-table/index.ts`.

## 3. Contents & Segments

- `ui/RolesTable.tsx`
  - `useRoles` with `useTableState({ defaultSortKey: 'role', defaultSortDirection: 'asc' })`; the
    grid runs in `mode="client"` with `rowSearchText` over the Arabic name, code, English name, and
    description, so `search`/`sort`/`page` stay in the URL and filtering is local.
  - Columns: role (`RoleBadge`, not hideable, sortable), description (two-line clamp with a title),
    permissions count badge (sortable), status badge, kind (نظام/مخصص), and a hidden, sortable
    `createdAt`.
  - Row menu: edit (role form), permission matrix, activate/deactivate (disabled for system roles),
    copy code, copy id. Header/empty menus: create role and refresh. Rows, headers, and the grid
    surface also open the same menu on right-click / `Shift+F10`.
  - Dialogs resolve the live row from the query cache by id, so a refreshed list cannot leave a
    stale permission snapshot open. Status changes go through `ConfirmDialog` with Arabic toasts.
- `index.ts` — Public API barrel file.

## 4. Required Packages

- `@tanstack/react-query`, `lucide-react`, `sonner`

## 5. Implementation Status

- ✅ Client search/sort/paging, column chooser, and Arabic empty/error states.
- ✅ Create/edit and permission-matrix dialogs mounted from the row/header menus.
- ✅ System-role deactivation disabled in the menu.
- ✅ Verified in-browser against the live API (RTL, light/dark, search, sorting, dialogs, matrix
  diff) without persisting mutations.

## 6. Change Log

- 2026-10-28: Implemented Phase 4 — the roles grid with permission-filtered actions, system-role
  protection, status confirmation, and the role/permissions dialogs; created this slice README.
- 2026-10-19: Documented role administration as a planned widget in `src/widgets/README.md`.
