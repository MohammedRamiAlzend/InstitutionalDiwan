# `src/features/user-manage` — User Account, Role & Department Assignment Feature Slice

> **Status:** Implemented (Module 02 Phase 3, 2026-10-28) — create/edit users, toggle account status, assign/revoke roles, and add/end department assignments. The dialogs degrade with Arabic warnings when the caller lacks the supporting catalog permission.

## 1. Purpose

Owns the write side of Identity user administration: Zod-validated forms, `useMutation` hooks, and the dialogs mounted by `widgets/users-table`. Every command maps to a live `/api/users` route and invalidates the owning query keys instead of hand-written strings.

## 2. Strict FSD Rules

- Allowed Imports: `@/entities/user`, `@/entities/department`, `@/entities/role`, `@/entities/employee`, `@/entities/lookup`, and `@/shared/*`.
- Forbidden Imports: NEVER import from `@/app`, `@/pages`, `@/widgets`, or sibling `@/features/*` slices.
- Public API: export the request types, form value types, mutation hooks, and the three dialogs via `src/features/user-manage/index.ts`.
- Menu entries, confirmations, and toolbar buttons belong to the mounting widget; this slice exports dialogs and mutations only.

## 3. Contents & Segments

### `model/types.ts`

- `CreateUserRequest` — `username`, `password`, `primaryDepartmentId`, `employeeId`, `scopeTypeId`, `mustChangePassword`.
- `UpdateUserRequest` — `primaryDepartmentId`, `employeeId`, `scopeTypeId`, `mustChangePassword` (username is immutable).
- `AssignDepartmentScopeRequest` — `departmentId`, `assignmentTypeId`, `includeSubtree`, `startDate`, `endDate`.

### `model/schemas.ts`

- `createUserFormSchema` / `updateUserFormSchema` share the username, department, employee, scope, and must-change-password rules; password is required (8–200 chars) only on create.
- `departmentAssignmentSchema` requires a department and assignment type and rejects an end date earlier than the start date.
- Exported inferred types: `UserFormValues`, `DepartmentAssignmentFormValues`.

### `api/userMutations.ts`

| Hook | Endpoint |
| --- | --- |
| `useCreateUserMutation` | `POST /api/users` |
| `useUpdateUserMutation` | `PUT /api/users/{id}` |
| `useSetUserActiveMutation` | `POST /api/users/{id}/activate` / `DELETE /api/users/{id}` |
| `useAssignUserRoleMutation` | `POST /api/users/{id}/roles` |
| `useRevokeUserRoleMutation` | `DELETE /api/users/{id}/roles/{roleId}` |
| `useAssignDepartmentScopeMutation` | `POST /api/users/{id}/department-assignments` |
| `useEndDepartmentScopeMutation` | `DELETE /api/users/{id}/department-assignments/{assignmentId}` |

`invalidateUsers` refreshes `userKeys.lists()` plus the affected user's detail and effective-permissions queries.

### `ui/UserFormDialog.tsx`

Create/edit form. Username is editable only on create; the temporary password field appears only on create. Department and employee pickers use `useDepartments` and `useEmployees({ page: 1, pageSize: 100 })`; the employee select uses a `none` sentinel because Radix forbids empty-string values. Scope options come from the `user-scope-types` lookup with Arabic fallbacks. Without `DEPARTMENT_MANAGE` the department picker is unavailable, a warning explains why, and submit is disabled.

### `ui/UserRolesDialog.tsx`

Lists the user's current role codes resolved to Arabic names via `useRoles`, allows revoking a role, and assigns an active unassigned role. The role catalog is gated on `ROLE_MANAGE`; without it the dialog keeps the current roles visible and shows an Arabic warning instead of the assign form.

### `ui/UserAccessDialog.tsx`

Read-only effective access plus department-assignment management:
- scope badge and role codes;
- effective permissions from `GET /api/users/{id}/effective-permissions`, grouped by module through the shared `permissionModuleLabel` from `entities/role`;
- active/ended assignments with type, subtree flag, dates, and an end action;
- add-assignment form (department, assignment type from `assignment-types`, `includeSubtree`, start/end dates).
Department and permission catalogs are permission-gated; missing catalogs show Arabic warnings rather than empty selects.

### `index.ts`

Public barrel for the request/form types, the seven mutation hooks, and the three dialogs.

## 4. Required Packages

- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`, `lucide-react`

## 5. Implementation Status

- ✅ Request types, Zod schemas, and all seven mutations implemented.
- ✅ Create/edit, roles, and access dialogs implemented with permission-aware degradation and Arabic validation/errors.
- ✅ Verified in-browser against the live API (edit prefill, Arabic validation on submit, role resolution, grouped effective permissions, assignment form).
- ❌ Password reset/force-change command is not exposed by the API; the temporary password is only set at creation.

## 6. Change Log

- 2026-10-28: Reused the shared `permissionModuleLabel` helper from `entities/role` instead of a local module-label map (Phase 4 consolidation).
- 2026-10-28: Implemented Phase 3 — request models, Zod schemas, and user/role/department-assignment mutations, plus the create/edit, roles, and access dialogs. Verified in-browser; build and lint pass.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/user-manage` slice folder and `README.md`.
