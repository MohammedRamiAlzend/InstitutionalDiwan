# institutionaldiwan.front / src/entities/user — User & Session Entity Slice
> **Status:** ✅ Implemented (2026-10-27) — session slice plus the Module 02 user admin reads (`GET /api/users`, detail, effective permissions).
## Purpose
Models Module 02 `User` (`MeResponse`, `UserResponse`) and owns the Zustand session store for the authenticated caller. Provides read queries for the caller profile and the user admin screens. Departments, roles, lookups, and employees now live in their own entity slices.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `MeResponse`, `UserResponse`, `UserListParams`, `UserScope`, `UserDepartmentAssignmentResponse`, `UserEffectivePermissionsResponse`, and `SessionUser` (camelCase JSON contracts). |
| `model/scope.ts` | `USER_SCOPE_TYPE_IDS` and `userScopeFromId(id)` mapping the backend scope ids (1=Department, 2=DepartmentTree, 3=Institution, 4=System) to `UserScope`. |
| `model/sessionStore.ts` | Zustand store holding the current `user`, `mustChangePassword` flag, `setSession`/`clear`, and `hasPermission(code)`. |
| `model/queries.ts` | `userKeys` (`all`/`me`/`lists`/`list(params)`/`detail`/`effectivePermissions`), `useMeQuery`, `useUsers`, `useUserById`, and `useUserEffectivePermissions`. |
| `api/userApi.ts` | Typed calls for `GET /api/auth/me`, paged `GET /api/users`, `GET /api/users/{id}`, and `GET /api/users/{id}/effective-permissions`. |
| `ui/UserScopeBadge.tsx` | Renders the Arabic scope label (الدائرة / الدائرة والفروع / المؤسسة / النظام) from `scopeTypeId`, with a `نطاق #id` fallback. |
| `index.ts` | Public barrel exporting types, session store, scope helpers, query hooks, and the badge. |
## Notes
- `useMeQuery` runs only while a token exists (`retry: false`); hydration into the store is owned by `AuthSessionLoader` in `src/app/providers`.
- List hooks require `USER_MANAGE`; the pages (Phase 3) own the permission gate and table state.
- Employee records moved to `entities/employee`, so this slice imports no sibling entities.
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `api/`, `ui/`, `index.ts` | ✅ Implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Implemented Phase 2 user admin reads (paged list, detail, effective permissions), the four-scope model with id mapping, and the id-driven `UserScopeBadge`; moved `EmployeeResponse` to `entities/employee`. |
| 2026-10-27 | agent | Implemented the session slice for Identity Module 01: typed contracts, Zustand store with `hasPermission`, `me` API/query, and the scope badge. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/user` slice folder and README. |
