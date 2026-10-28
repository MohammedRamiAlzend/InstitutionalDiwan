# `src/widgets/employees-table` — Employees Administration Grid Widget

> **Status:** Implemented (Module 02 Phase 5, 2026-10-28) — server-paged employees grid with status actions and the create/edit dialog.

## 1. Purpose

Composes the employee reads (`entities/employee`) with the employee writes
(`features/employee-manage`) into the `/employees` administration surface: the shared data grid,
debounced server search, paging, a column chooser, and row/header/empty context menus.

## 2. Strict FSD Rules

- Allowed Imports: `@/features/employee-manage`, `@/entities/{employee,department}`, `@/entities/user`, and `@/shared/*`.
- Forbidden Imports: NEVER import from `@/app`, `@/pages`, or sibling `@/widgets/*` slices.
- Public API: export `EmployeesTable` via `src/widgets/employees-table/index.ts`.

## 3. Contents & Segments

- `ui/EmployeesTable.tsx`
  - `useEmployees` driven by `useTableState` (`page`, `pageSize`, `search`, `sortKey`,
    `sortDirection`, and a `departmentId` filter in the URL; default `fullNameAr` ascending); the API
    searches the Arabic name, English name, and employee number.
  - Columns: employee (Arabic name with the English name underneath), employee number (LTR mono),
    department (`DepartmentBadge` when the department catalog is available, otherwise a shortened GUID
    with the full value in `title`), job title, email/phone (hidden by default, LTR), status, and
    hidden created/updated timestamps. Employee name, employee number, status, and creation time are
    sortable through the server; the toolbar filters by department (including all departments).
  - Department labels come from `useDepartmentOptions` (`/api/identity-options/departments`, enabled
    by `EMPLOYEE_MANAGE`), with an Arabic retry action when that read fails.
  - Row menu: edit, activate/deactivate, copy employee number (disabled when absent), copy id.
    Header/empty menus and the toolbar offer create + refresh; rows, headers, and the grid surface
    also open the menu on right-click / `Shift+F10`.
  - Status changes go through `ConfirmDialog` with Arabic toasts.
- `index.ts` — Public API barrel file.

## 4. Required Packages

- `@tanstack/react-query`, `lucide-react`, `sonner`

## 5. Implementation Status

- ✅ Server search/paging, column chooser, status confirmation, and Arabic empty/error states.
- ✅ Create/edit dialog mounted from the toolbar and row/header menus.
- ✅ Verified in-browser against the live API (RTL, light/dark, create, search, edit prefill,
  status toggle).
- ✅ Server-side sorting (name, employee number, status, creation time) and a department filter are
  wired through the grid toolbar and `GET /api/employees?sortBy=…&sortDescending=…&departmentId=…`.

## 6. Change Log

- 2026-10-28: Added server sorting and a department filter, and switched department labels to the
  minimal `useDepartmentOptions` catalog so the grid works with `EMPLOYEE_MANAGE` alone.

- 2026-10-28: Implemented Phase 5 — the employees grid with department resolution/fallback,
  permission-aware row actions, status confirmation, and the create/edit dialog; created this README.
