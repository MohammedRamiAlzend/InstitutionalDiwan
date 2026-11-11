# institutionaldiwan.front / src/entities/user — User & Session Entity Slice
> **Status:** ✅ Implemented (2026-10-28) — session slice, the Module 02 user admin reads (`GET /api/users`, detail, effective permissions), and the minimal `identity-options` user picker (`useUserOptions`).
## Purpose
Models Module 02 `User` (`MeResponse`, `UserResponse`, `UserOption`) and owns the Zustand session store for the authenticated caller. Provides read queries for the caller profile, the user admin screens, and the pickers that need a user id (document access grants). Departments, roles, lookups, and employees now live in their own entity slices.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `MeResponse`, `UserResponse`, `UserListParams`, `UserOption`, `UserOptionParams`, `UserScope`, `UserDepartmentAssignmentResponse`, `UserEffectivePermissionsResponse`, and `SessionUser` (camelCase JSON contracts). |
| `model/scope.ts` | `USER_SCOPE_TYPE_IDS` and `userScopeFromId(id)` mapping the backend scope ids (1=Department, 2=DepartmentTree, 3=Institution, 4=System) to `UserScope`. |
| `model/sessionStore.ts` | Zustand store holding the current `user`, `mustChangePassword` flag, `setSession`/`clear`, and `hasPermission(code)`. |
| `model/queries.ts` | `userKeys` (`all`/`me`/`departmentScope`/`lists`/`list(params)`/`optionLists`/`options(params)`/`detail`/`effectivePermissions`), `useMeQuery`, `useCoversDepartment`, `useUsers`, `useUserById`, `useUserEffectivePermissions`, and `useUserOptions`. |
| `api/userApi.ts` | Typed calls for `GET /api/auth/me`, paged `GET /api/users`, `GET /api/users/{id}`, `GET /api/users/{id}/effective-permissions`, and the paged `GET /api/identity-options/users` picker. |
| `api/userLabelLoader.ts` | `loadUserLabel(userId)`: collects every id requested in the same tick and resolves them with one `GET /api/identity-options/users/by-ids` call (chunks of 100). A failed or forbidden read resolves `null` so the label falls back to the short id. |
| `ui/UserLabel.tsx` | `UserLabel`: username plus short id for a user reference (`userKeys.label(id)`, 10 min stale time); used by tables and dialogs instead of a bare id. |
| `ui/UserScopeBadge.tsx` | Renders the Arabic scope label (الدائرة / الدائرة والفروع / المؤسسة / النظام) from `scopeTypeId`, with a `نطاق #id` fallback. |
| `index.ts` | Public barrel exporting types, session store, scope helpers, query hooks, and the badge. |
## Notes
- `useMeQuery` runs only while a token exists (`retry: false`); hydration into the store is owned by `AuthSessionLoader` in `src/app/providers`.
- `useCoversDepartment` reads `accessibleDepartmentIds` from `GET /api/auth/me` under its own key: `AuthSessionLoader` re-syncs the store and resets `mustChangePassword` whenever `userKeys.me()` receives data, so that key must stay disabled while a session is loaded. Nothing is covered until the profile loads.
- List hooks require `USER_MANAGE`; the pages (Phase 3) own the permission gate and table state.
- `useUserOptions` reads the minimal picker (`userId`, `username`, `isActive`) behind `USER_MANAGE` OR `ACCESS_GRANT_MANAGE` OR `DOCUMENT_EDIT`. It includes inactive accounts so existing references still label; callers filter them out for new selections.
- Employee records moved to `entities/employee`, so this slice imports no sibling entities.
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `api/`, `ui/`, `index.ts` | ✅ Implemented |
| `useUserOptions` picker read | ✅ Implemented (Module 03 Phase 4) |
| `useCoversDepartment` scope read | ✅ Implemented and browser-verified (department-scoped sender, receiver, unrelated user) |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude |  |
| 2026-11-04 | Claude | Added `UserLabel` and the batching `loadUserLabel` loader (`userKeys.label`) so «executed by» style columns show the username with the short id. |
| 2026-11-04 | Claude | `MeResponse.primaryDepartmentId` and the `useMyPrimaryDepartmentId` hook (returns `{ departmentId, isLoading }`; shares the scope query key with `useCoversDepartment`). |
| 2026-11-03 | Claude | Added `accessibleDepartmentIds` to `MeResponse` and `useCoversDepartment`, which answers whether the caller's scope reaches a department. |
| 2026-10-28 | agent | Module 03 Phase 4: added `UserOption`/`UserOptionParams`, the paged `GET /api/identity-options/users` call, `optionLists`/`options` query keys, and `useUserOptions` for the document access-grant picker. |
| 2026-10-27 | agent | Implemented the session slice for Identity Module 01: typed contracts, Zustand store with `hasPermission`, `me` API/query, and the scope badge. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/user` slice folder and README. |
