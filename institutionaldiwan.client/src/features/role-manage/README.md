# `src/features/role-manage` — Role & Permission Matrix Management Feature Slice
## 1. Purpose
Provides interactive role creation and granular permission matrix toggling (`POST /api/roles`, `PUT /api/roles/{id}/permissions`) mapped to `PermissionsCatalog`.
## 2. Strict FSD Rules
- Allowed Imports: `src/entities/role` and `src/shared/*`.
- Forbidden Imports: NEVER import from `src/app`, `src/pages`, `src/widgets`, or sibling `src/features/*` slices.
- Public API: Export `RolePermissionMatrixForm`, `CreateRoleDialog`, and role mutations via `src/features/role-manage/index.ts`.
## 3. Planned Contents & Segments
- `ui/RolePermissionMatrixForm.tsx` — Grouped checkbox matrix for assigning permissions to a role.
- `ui/CreateRoleDialog.tsx` — Modal to create a new role.
- `model/role.schema.ts` — Zod validation schema for role creation and permission assignment.
- `api/use-role-mutations.ts` — TanStack Query mutations invalidating `roleKeys.all`.
- `index.ts` — Public API barrel file.
## 4. Required Packages
- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`
## 5. Implementation Status
- Slice folder and FSD rules established
- Implement `RolePermissionMatrixForm` and role mutations
## 6. Change Log
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/role-manage` slice folder and `README.md`.
