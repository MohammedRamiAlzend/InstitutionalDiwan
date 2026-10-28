# `src/features/role-manage` — Role Administration & Permission Matrix Feature Slice

> **Status:** Implemented (Module 02 Phase 4, 2026-10-28) — role create/edit, activate/deactivate, and the staged permission-matrix diff.

## 1. Purpose

Owns the Module 02 role writes (`ROLE_MANAGE`): creating and renaming roles, activating or
deactivating them, and granting/revoking permissions from the catalog. The dialogs are mounted by
`widgets/roles-table`; the slice itself never renders a page.

## 2. Strict FSD Rules

- Allowed Imports: `@/entities/role` and `@/shared/*`.
- Forbidden Imports: NEVER import from `@/app`, `@/pages`, `@/widgets`, or sibling `@/features/*` slices.
- Public API: request types, `RoleFormValues`, the four mutations, `RoleFormDialog`, and
  `RolePermissionsDialog` via `src/features/role-manage/index.ts`.

## 3. Contents & Segments

| File | Purpose |
| --- | --- |
| `model/types.ts` | `CreateRoleRequest` (`code`, `nameAr`, `nameEn`, `description`), `UpdateRoleRequest` (code is immutable), and `UpdateRolePermissionsRequest` (`{ roleId, grant, revoke }`). |
| `model/schemas.ts` | `roleFormSchema` — required trimmed `code`/`nameAr` (100/150), optional `nameEn` (150) and `description` (500), with Arabic messages. |
| `api/roleMutations.ts` | `useCreateRoleMutation` (`POST /api/roles`), `useUpdateRoleMutation` (`PUT /api/roles/{id}`), `useSetRoleActiveMutation` (`POST /{id}/activate` or `DELETE /{id}`), and `useUpdateRolePermissionsMutation`. Every hook calls `invalidateRoles` (list + optional detail) through the entity key factory; permission updates invalidate in `onSettled` so a partial sequential failure still reconciles the grid. |
| `ui/RoleFormDialog.tsx` | Create/edit dialog from `roleFormSchema`; the code field is disabled when editing, system roles show a protection note, and success/failure surface as Arabic toasts and an inline `toArabicErrorMessage` message. |
| `ui/RolePermissionsDialog.tsx` | Permission matrix grouped by `moduleName` through `permissionModuleLabel`; the catalog is fetched with `usePermissionsCatalog`, each row shows the Arabic permission name plus an LTR code chip, and the footer saves only the computed grant/revoke diff (`إعادة التعيين`/`إغلاق`/`حفظ التغييرات`, disabled while clean or pending). |
| `index.ts` | Public API barrel file. |

## 4. Required Packages

- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`

## 5. Implementation Status

- ✅ Create/edit and activate/deactivate mutations wired to the live API.
- ✅ Permission matrix staged diff (grant then revoke) with reset, clean-state guard, and partial-failure reconciliation.
- ✅ System roles remain editable but cannot be deactivated in the UI (backend `Identity.SystemRoleProtected`).
- ✅ Verified in-browser against the live API (RTL, light/dark, Arabic validation, matrix toggles) without persisting changes.

## 6. Change Log

- 2026-10-28: Implemented Phase 4 — role request types, Zod schema, four mutations, the create/edit dialog, and the grouped permission-matrix dialog; rewrote this README to match the code.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/role-manage` slice folder and `README.md`.
