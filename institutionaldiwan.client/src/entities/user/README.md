# institutionaldiwan.client / src/entities/user — User, Session & Employee Entity Slice
> **Status:** Scaffolding & Rules — User/Employee read models, auth session store, and queries defined; code pending.
## Purpose
Models Module 02 `User` (`UserResponse`, `MeResponse`) and `Employee` (`EmployeeResponse`). Owns the Zustand session store for the currently authenticated caller (`GET /api/auth/me`), plus read queries for `GET /api/users`, `GET /api/users/{id}`, `GET /api/employees`, and `GET /api/employees/{id}`.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `MeResponse`, `UserResponse`, and `EmployeeResponse` TypeScript contracts. |
| `model/sessionStore.ts` | Zustand store holding current `user`, `permissions` Set, `mustChangePassword` flag, and `hasPermission(code)` helper. |
| `model/queries.ts` | `userKeys` & `employeeKeys` factories plus `useMeQuery`, `useUsers`, `useUserById`, `useEmployees`, `useEmployeeById`. |
| `api/userApi.ts` | Typed `GET` calls to `/api/auth/me`, `/api/users`, and `/api/employees`. |
| `ui/UserScopeBadge.tsx` | Displays `Department`, `Subtree`, or `System` scope badge. |
| `index.ts` | Public barrel exporting types, session store, query hooks, and badges. |
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `api/`, `ui/`, `index.ts` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/user` slice folder and README. |
