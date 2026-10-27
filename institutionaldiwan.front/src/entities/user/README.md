# institutionaldiwan.front / src/entities/user — User, Session & Employee Entity Slice
> **Status:** Session parts implemented for Identity Module 01 (`types`, `sessionStore`, `userApi.me`, `useMeQuery`, `UserScopeBadge`); the user/employee list queries land with the admin pages.
## Purpose
Models Module 02 `User` (`UserResponse`, `MeResponse`) and `Employee` (`EmployeeResponse`). Owns the Zustand session store for the currently authenticated caller (`GET /api/auth/me`), plus read queries for the user and employee admin screens (pending).
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `MeResponse`, `UserResponse`, `EmployeeResponse`, `UserScope`, and the `SessionUser` shape held by the store (camelCase JSON contracts). |
| `model/sessionStore.ts` | Zustand store holding the current `user`, `mustChangePassword` flag, `setSession`/`clear`, and `hasPermission(code)`. |
| `model/queries.ts` | `userKeys` factory and `useMeQuery` (runs while a token exists; hydration is owned by `AuthSessionLoader`). `useUsers` / `useUserById` land with the users page. |
| `api/userApi.ts` | Typed `GET /api/auth/me` call; user/employee list calls land with their pages. |
| `ui/UserScopeBadge.tsx` | Displays the `System`, `Department`, or `Subtree` scope badge. |
| `index.ts` | Public barrel exporting types, session store, query hooks, and badges. |
## Implementation status
| Item | State |
| --- | --- |
| `model/types.ts`, `model/sessionStore.ts`, `api/userApi.ts`, `model/queries.ts` (`useMeQuery`), `ui/UserScopeBadge.tsx`, `index.ts` | ✅ Implemented |
| `useUsers`, `useUserById`, `useEmployees`, `useEmployeeById` | ❌ Pending the users/employees pages |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Implemented the session slice for Identity Module 01: typed contracts, Zustand store with `hasPermission`, `me` API/query, and the scope badge. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/user` slice folder and README. |
