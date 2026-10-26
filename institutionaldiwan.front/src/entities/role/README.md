# institutionaldiwan.front / src/entities/role — Role & Permission Catalog Entity Slice
> **Status:** Scaffolding & Rules — Role and Permission read models and queries defined; code pending.
## Purpose
Models Module 02 `Role` (`RoleResponse`) and `Permission` (`PermissionResponse`). Owns `GET /api/roles`, `GET /api/roles/{roleId}`, and `GET /api/permissions` queries and read-only role/permission chips.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `RoleResponse` and `PermissionResponse` TypeScript contracts. |
| `model/queries.ts` | `roleKeys` & `permissionKeys` factories plus `useRoles`, `useRoleById`, `usePermissionsCatalog` hooks. |
| `api/roleApi.ts` | Typed `GET` calls to `/api/roles` and `/api/permissions`. |
| `ui/RoleBadge.tsx` | Renders role `nameAr` / `code` with a system-role lock indicator when `isSystemRole === true`. |
| `index.ts` | Public barrel exporting types, query keys, hooks, and UI components. |
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `api/`, `ui/`, `index.ts` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/role` slice folder and README. |
