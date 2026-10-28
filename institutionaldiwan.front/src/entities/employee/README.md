# institutionaldiwan.front / src/entities/employee — Employee Record Entity Slice
> **Status:** Paged list/detail reads, server sorting, permission-scoped picker reads, and the reusable `EmployeePicker` are implemented and verified in the department, user, and institution-settings forms.
## Purpose
Models Module 02 `Employee` (`EmployeeResponse`) as its own entity slice; writes belong to `features/employee-manage`. Owns `GET /api/employees` (server-side paging, name/number search, and sorting), `GET /api/employees/{id}`, the minimal `GET /api/identity-options/employees` picker reads, and the employee selector reused by department, user, and institution-settings forms.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `EmployeeResponse` and `EmployeeListParams` (`page`, `pageSize`, optional `search`, `departmentId`, `includeSubDepartments`, `sortBy`, `sortDescending`). |
| `api/employeeApi.ts` | Paged `GET /api/employees` (trims `search`, omits it when empty; forwards `departmentId` and `includeSubDepartments` only when a department is selected; forwards `sortBy`/`sortDescending`) and `GET /api/employees/{id}`. |
| `model/options.ts` | `useEmployeeOptions` (paged `GET /api/identity-options/employees`) and `useEmployeeOptionById` for labels only — no contact data, accepted by `USER_MANAGE` **or** `DEPARTMENT_MANAGE` **or** `EMPLOYEE_MANAGE`. |
| `model/queries.ts` | `employeeKeys` (`all`/`lists`/`list(params)`/`detail`), `useEmployees` (keeps previous page via `placeholderData`, 60 s stale time), and `useEmployeeById` (disabled without an id). |
| `ui/EmployeePicker.tsx` | Controlled nullable employee selector with 300 ms debounced search, 20-item pages, selected-record lookup, and Arabic list-error retry. |
| `index.ts` | Public barrel exporting types, query keys, hooks, and `EmployeePicker`. |
## Notes
- `EmployeeResponse` previously lived in `entities/user`; it moved here so `entities/user` no longer owns employee records and no sibling entity import is needed.
- Listing and reading require `EMPLOYEE_MANAGE`; `pages/employees` owns the permission gate, and the
  department roster in `widgets/department-hierarchy-tree` hides itself without that permission.
- `departmentId` + `includeSubDepartments` are the server-side department filter: the widget sends
  `true` by default so a directorate lists everyone under it.
- `EmployeePicker` reads the minimal `/api/identity-options/employees` contract, so `USER_MANAGE`
  forms work without `EMPLOYEE_MANAGE`; the roster grid keeps using the full detail endpoint.
- `EmployeePicker` accepts a nullable value plus change callback and labels. Its search/page are local
  form state, not a route grid. A selected employee outside the current page stays available through
  the dedicated option-by-id read or short-ID fallback. Inactive options are disabled unless already
  selected.
- Callers supply `disabled` when lacking permission; disabled pickers perform neither list nor detail
  reads. Department-manager and responsible-employee forms retain their loaded value in that state.
  List errors have retry; a failed selected-detail read falls back to the identifier label.
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `api/`, `index.ts` | Implemented. |
| `ui/EmployeePicker.tsx` | Implemented and verified in the department, user, and institution-settings forms. |
| `features/employee-manage` (create/edit/activate) | Implemented (Phase 5). |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Added `model/options.ts` for the permission-scoped `identity-options` picker reads, documented list sorting, and recorded picker verification in the department, user, and settings forms. |
| 2026-10-28 | agent | Document the reusable paged/searchable `EmployeePicker`, selected-value fallback, inactive options, and caller-controlled permission gating for Module 01 forms. |
| 2026-10-28 | agent | `EmployeeListParams` gained `departmentId` and `includeSubDepartments`, forwarded by `employeeApi.list`, so the department dialog can request one unit's roster (optionally including descendants). |

| 2026-10-28 | agent | Phase 5 landed `features/employee-manage` and the `/employees` page on top of these reads; the slice itself is unchanged. |
| 2026-10-27 | agent | Created the slice for Module 02 Phase 2 (structure B) with paged list/detail reads moved out of `entities/user`; live API shape verified. |
