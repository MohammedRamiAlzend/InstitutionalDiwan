# institutionaldiwan.front / src/entities/role — Role & Permission Catalog Entity Slice
> **Status:** ✅ Implemented (2026-10-27; module-label helper added 2026-10-28) — role list/detail, permission catalog, `RoleBadge`, and the shared permission-module labels.
## Purpose
Models Module 02 `Role` (`RoleResponse`) and `Permission` (`PermissionResponse`). Owns `GET /api/roles`, `GET /api/roles/{roleId}`, and `GET /api/permissions`, and provides a read-only role chip.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `RoleResponse` (including `permissions: PermissionCode[]`) and `PermissionResponse` contracts. |
| `model/queries.ts` | `roleKeys` (`all`/`list`/`detail`) and `permissionKeys` (`all`/`list`), plus `useRoles`, `useRoleById` (guarded by id), and `usePermissionsCatalog` (`staleTime: Infinity`). |
| `api/roleApi.ts` | Typed `GET` calls to `/api/roles`, `/api/roles/{id}`, and `/api/permissions`. |
| `ui/RoleBadge.tsx` | Renders role `nameAr` and LTR `code`, with a lock (`aria-label="دور معرّف من النظام"`) when `isSystemRole` is true. |
| `model/moduleLabels.ts` | `PERMISSION_MODULE_LABELS` and `permissionModuleLabel(moduleName)` — the single Arabic mapping (`IDENTITY` → الهوية والوصول, …) shared by the user access dialog and the role permission matrix; unknown modules fall back to the raw name. |
| `index.ts` | Public barrel exporting types, query keys, hooks, the module-label helper, and the badge. |
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `api/`, `ui/`, `index.ts` | ✅ Implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Added `model/moduleLabels.ts` so the user access dialog and the role permission matrix share one Arabic module-label mapping; removed the duplicated map from `features/user-manage`. |
| 2026-10-27 | agent | Implemented Phase 2 role and permission-catalog reads plus `RoleBadge`; live API shapes verified. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/role` slice folder and README. |
