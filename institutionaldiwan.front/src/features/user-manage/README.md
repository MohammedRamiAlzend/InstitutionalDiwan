# `src/features/user-manage` — User Account, Role & Department Assignment Feature Slice

> **Status:** Implemented (Module 02 Phase 3, 2026-10-28; admin password reset added 2026-10-28) — create/edit users, toggle account status, assign/revoke roles, add/end department assignments, and reset a forgotten password. The dialogs degrade with Arabic warnings when the caller lacks the supporting catalog permission.

## 1. Purpose

Owns the write side of Identity user administration: Zod-validated forms, `useMutation` hooks, and the dialogs mounted by `widgets/users-table`. Every command maps to a live `/api/users` route and invalidates the owning query keys instead of hand-written strings.

## 2. Strict FSD Rules

- Allowed Imports: `@/entities/user`, `@/entities/department`, `@/entities/role`, `@/entities/employee`, `@/entities/lookup`, and `@/shared/*`.
- Forbidden Imports: NEVER import from `@/app`, `@/pages`, `@/widgets`, or sibling `@/features/*` slices.
- Public API: export the request types, form value types, mutation hooks, and the four dialogs via `src/features/user-manage/index.ts`.
- Menu entries, confirmations, and toolbar buttons belong to the mounting widget; this slice exports dialogs and mutations only.

## 3. Contents & Segments

### `model/types.ts`

- `CreateUserRequest` — `username`, `password`, `primaryDepartmentId`, `employeeId`, `scopeTypeId`, `mustChangePassword`.
- `UpdateUserRequest` — `primaryDepartmentId`, `employeeId`, `scopeTypeId`, `mustChangePassword` (username is immutable).
- `ResetUserPasswordRequest` — `newPassword` (temporary password handed to the owner).
- `AssignDepartmentScopeRequest` — `departmentId`, `assignmentTypeId`, `includeSubtree`, `startDate`, `endDate`.

### `model/schemas.ts`

- `createUserFormSchema` / `updateUserFormSchema` share the username, department, employee, scope, and must-change-password rules; password is required (8–200 chars) only on create.
- `departmentAssignmentSchema` requires a department and assignment type and rejects an end date earlier than the start date.
- `resetUserPasswordSchema` requires a temporary password of 8–200 characters plus a matching confirmation.
- Exported inferred types: `UserFormValues`, `DepartmentAssignmentFormValues`, `ResetUserPasswordFormValues`.

### `api/userMutations.ts`

| Hook | Endpoint |
| --- | --- |
| `useCreateUserMutation` | `POST /api/users` |
| `useUpdateUserMutation` | `PUT /api/users/{id}` |
| `useSetUserActiveMutation` | `POST /api/users/{id}/activate` / `DELETE /api/users/{id}` |
| `useResetUserPasswordMutation` | `POST /api/users/{id}/reset-password` |
| `useAssignUserRoleMutation` | `POST /api/users/{id}/roles` |
| `useRevokeUserRoleMutation` | `DELETE /api/users/{id}/roles/{roleId}` |
| `useAssignDepartmentScopeMutation` | `POST /api/users/{id}/department-assignments` |
| `useEndDepartmentScopeMutation` | `DELETE /api/users/{id}/department-assignments/{assignmentId}` |

`invalidateUsers` refreshes `userKeys.lists()` plus the affected user's detail and effective-permissions queries.

### `ui/UserFormDialog.tsx`

Create/edit form. Username is editable only on create; the temporary password field appears only on create. The department picker uses `useDepartmentOptions` (`/api/identity-options/departments`) and the employee link uses the shared paged `EmployeePicker` (`/api/identity-options/employees`, `none` = no employee), so neither read requires a permission the user administrator does not hold. Scope options come from the `user-scope-types` lookup with Arabic fallbacks; both option reads start only while the dialog is open.

### `ui/UserRolesDialog.tsx`

Reads the current roles from a fresh `GET /api/users/{id}` each time it opens (never the possibly stale table row), resolves them via the minimal `useRoleOptions` catalog, allows revoking a role, and assigns an active unassigned role. Gated on `USER_MANAGE`, matching the server policy on `/api/users/{id}/roles`.

### `ui/UserAccessDialog.tsx`

Read-only effective access plus department-assignment management:
- scope badge and role codes;
- effective permissions from `GET /api/users/{id}/effective-permissions`, grouped by module through the shared `permissionModuleLabel` from `entities/role`;
- the assignment history from a fresh `GET /api/users/{id}` (scheduled / active / ended / manually ended, type, subtree flag, dates, end action);
- add-assignment form (department, assignment type from `assignment-types`, `includeSubtree`, start/end dates).
The department catalog comes from the minimal `useDepartmentOptions`; unsupported or inactive scope/type inputs are rejected server-side with `Identity.UserScopeTypeInvalid`.

### `ui/ResetPasswordDialog.tsx`

Administrative reset for a forgotten password: a warning explains that the old password stops working immediately, a «توليد كلمة مرور مؤقتة» action fills both fields with a random 12-character password (`crypto.getRandomValues`, ambiguous characters excluded), and two shared `PasswordField`s capture and confirm the temporary password. On success the dialog toasts that the user must choose a new password at the next sign-in and closes; failures render through the Arabic error catalog.

### `index.ts`

Public barrel for the request/form types, the eight mutation hooks, and the four dialogs.

## 4. Required Packages

- `@tanstack/react-query`, `react-hook-form`, `zod`, `@hookform/resolvers`, `sonner`, `lucide-react`

## 5. Implementation Status

- ✅ Request types, Zod schemas, and all eight mutations implemented.
- ✅ Create/edit, roles, access, and password-reset dialogs implemented with permission-aware degradation and Arabic validation/errors.
- ✅ Verified in-browser against the live API (edit prefill, Arabic validation on submit, role resolution, grouped effective permissions, assignment form).
- ✅ Admin password reset implemented against `POST /api/users/{id}/reset-password`; the reset flags the account for a forced change at the next sign-in.
- ✅ Verified in-browser: the row action is disabled for the signed-in account, empty submit shows Arabic field errors, the generator fills both fields with the same 12-character password, success closes the dialog with a toast, and the reset user's next sign-in lands on `/change-password`.
- ✅ Authorization alignment: the roles/access dialogs and the users grid gate on `USER_MANAGE` (the server policy) instead of the unrelated `ROLE_MANAGE`/`DEPARTMENT_MANAGE`, and reads go through the minimal `identity-options` catalog.

## 6. Change Log

- 2026-10-28: Replaced list-based pickers with `useDepartmentOptions`/`EmployeePicker`; the roles and access dialogs now refetch the user on open instead of trusting the table row; assignment rows distinguish scheduled, active, ended-by-date, and manually ended states; all user actions gate on `USER_MANAGE`.

- 2026-10-28: Added the administrator password reset (`ResetPasswordDialog`, `resetUserPasswordSchema`, `useResetUserPasswordMutation`) against the new `POST /api/users/{id}/reset-password`; passwords use the new shared `PasswordField`. Verified in-browser end to end (API 200/400/401 responses, Arabic validation, forced change on next login, dialog bounded in both themes).
- 2026-10-28: The access/assignment dialog now relies on the dialog primitive's bounded body: title and footer stay pinned, the permissions and assignments regions scroll together inside a viewport-capped panel.
- 2026-10-28: Reused the shared `permissionModuleLabel` helper from `entities/role` instead of a local module-label map (Phase 4 consolidation).
- 2026-10-28: Implemented Phase 3 — request models, Zod schemas, and user/role/department-assignment mutations, plus the create/edit, roles, and access dialogs. Verified in-browser; build and lint pass.
- 2026-10-22: Converted this `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable.
- 2026-10-19: Created `features/user-manage` slice folder and `README.md`.
