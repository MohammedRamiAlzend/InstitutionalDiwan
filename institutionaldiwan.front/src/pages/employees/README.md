# `src/pages/employees` — Employees Administration Page Slice (`/employees`)

> **Status:** Implemented (Module 02 Phase 5, 2026-10-28) — the `/employees` screen gates on `EMPLOYEE_MANAGE` and mounts the employees administration grid.

## 1. Purpose

Renders the employee administration route. The page is a thin composition unit: it owns the
route-level permission gate and the Arabic page header, and delegates the table, dialogs, and row
actions to `widgets/employees-table`.

## 2. Strict FSD Rules

- Allowed Imports: `src/widgets/*`, `src/features/*`, `src/entities/*`, and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app` or sibling `src/pages/*` slices.
- Public API: Export `EmployeesPage` only via `src/pages/employees/index.ts`.

## 3. Contents & Segments

- `ui/EmployeesPage.tsx` — `RequirePermission(EMPLOYEE_MANAGE)` wrapper, page header
  («إدارة الموظفين»), and `<EmployeesTable />`.
- `index.ts` — Public API barrel file.

## 4. Required Packages

- `lucide-react` (via the page header and the widget)

## 5. Implementation Status

- ✅ `/employees` route mounted inside the app shell and gated on `EMPLOYEE_MANAGE` (Arabic forbidden state otherwise).
- ✅ Employees grid, create/edit dialog, server search, and status confirmation verified in-browser.

## 6. Change Log

- 2026-10-28: Implemented Phase 5 — the `/employees` page mounts `widgets/employees-table` behind the
  `EMPLOYEE_MANAGE` guard; created this slice README.
