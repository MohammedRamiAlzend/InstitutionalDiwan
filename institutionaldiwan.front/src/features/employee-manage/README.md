# `src/features/employee-manage` — Employee Record Administration Feature Slice

> **Status:** Implemented (Module 02 Phase 5, 2026-10-28) — employee create/edit and activate/deactivate.

## 1. Purpose

Owns the Module 02 employee writes (`EMPLOYEE_MANAGE`) for `POST /api/employees`,
`PUT /api/employees/{id}`, `DELETE /api/employees/{id}`, and `POST /api/employees/{id}/activate`.
The form dialog is mounted by `widgets/employees-table`; the slice renders no page.

## 2. Strict FSD Rules

- Allowed Imports: `@/entities/{employee,department}`, `@/entities/user` (session permissions), and `@/shared/*`.
- Forbidden Imports: NEVER import from `@/app`, `@/pages`, `@/widgets`, or sibling `@/features/*` slices.
- Public API: request types, `EmployeeFormValues`, the three mutations, and `EmployeeFormDialog`
  via `src/features/employee-manage/index.ts`.

## 3. Contents & Segments

| File | Purpose |
| --- | --- |
| `model/types.ts` | `CreateEmployeeRequest` and `UpdateEmployeeRequest`: required `fullNameAr`/`departmentId`; optional `employeeNumber`, `fullNameEn`, `jobTitleAr`, `email`, `phone` (sent as `null` when empty). |
| `model/schemas.ts` | `employeeFormSchema` mirroring the server limits (name 200, number 50, English name 200, job title 150, email 150 + format, phone 50) with Arabic messages. |
| `api/employeeMutations.ts` | `useCreateEmployeeMutation`, `useUpdateEmployeeMutation`, and `useSetEmployeeActiveMutation`; each calls `invalidateEmployees` (list + detail via `employeeKeys`). |
| `ui/EmployeeFormDialog.tsx` | Create/edit dialog. The department picker is enabled with `DEPARTMENT_MANAGE`; on edit the current department is injected as a fallback option so a caller without department reads can still edit other fields. Create is blocked without a department catalog, with an Arabic explanation. |
| `index.ts` | Public API barrel file. |

## 4. Required Packages

- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`, `lucide-react`

## 5. Implementation Status

- ✅ Create, update, activate, and deactivate wired to the live API.
- ✅ Arabic validation for every field, including `Identity.EmployeeEmailInvalid` / `Identity.EmployeeDepartmentEmpty` in the shared catalog.
- ✅ Verified in-browser against the live API (create, edit prefill, invalid email, status confirmation, light/dark).

## 6. Change Log

- 2026-10-28: Implemented Phase 5 — employee request types, Zod schema, three mutations, and the create/edit dialog; created this README.
