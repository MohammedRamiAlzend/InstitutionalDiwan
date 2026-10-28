# institutionaldiwan.front / src/entities/employee — Employee Record Entity Slice
> **Status:** ✅ Implemented (2026-10-27; employee writes landed in Phase 5 on 2026-10-28) — paged list and detail reads.
## Purpose
Models Module 02 `Employee` (`EmployeeResponse`) as its own entity slice (structure B: read model in `entities/employee`, write flows in a future `features/employee-manage`). Owns `GET /api/employees` (server-side paging + name/number search) and `GET /api/employees/{id}`.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `EmployeeResponse` and `EmployeeListParams` (`page`, `pageSize`, optional `search`). |
| `api/employeeApi.ts` | Paged `GET /api/employees` (trims `search`, omits it when empty) and `GET /api/employees/{id}`. |
| `model/queries.ts` | `employeeKeys` (`all`/`lists`/`list(params)`/`detail`), `useEmployees` (keeps previous page via `placeholderData`, 60 s stale time), and `useEmployeeById` (disabled without an id). |
| `index.ts` | Public barrel exporting types, query keys, and hooks. |
## Notes
- `EmployeeResponse` previously lived in `entities/user`; it moved here so `entities/user` no longer owns employee records and no sibling entity import is needed.
- Listing and reading require `EMPLOYEE_MANAGE`; `pages/employees` owns the permission gate.
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `api/`, `index.ts` | ✅ Implemented |
| `features/employee-manage` (create/edit/activate) | ✅ Implemented (Phase 5) |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Phase 5 landed `features/employee-manage` and the `/employees` page on top of these reads; the slice itself is unchanged. |
| 2026-10-27 | agent | Created the slice for Module 02 Phase 2 (structure B) with paged list/detail reads moved out of `entities/user`; live API shape verified. |
