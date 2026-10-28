# Application / Features / Identity / Users — user administration and department scope

> **Status:** Implemented — CRUD, activation, role grants, temporal department assignments, and the
> effective-permissions projection.

## Purpose

Every use case behind `/api/users`: creating and updating accounts, deactivating/activating them,
granting and revoking roles, assigning a temporal department scope (a `UserDepartmentAssignment` that
widens where the user may work, optionally covering the department subtree), ending that assignment
without deleting the history row, and resolving a user's effective permissions — the same permission
set their access token carries.

## Contents

| File | Purpose |
| --- | --- |
| `UserResponse.cs` | Summary read model (id, username, employee, department, scope, flags, timestamps, role codes). |
| `UserResponseFactory.cs` | Shared projection: resolves each `UserRole` to its role code via `IRoleRepository`, skips stale grants, sorts codes. |
| `ListUsersQuery.cs` / `ListUsersQueryHandler.cs` | Returns one page of accounts (`PagedResult<UserResponse>`) ordered by username, with a `Search` term against the username and `Page`/`PageSize` input; roles are resolved for the page only. |
| `GetUserByIdQuery.cs` / `GetUserByIdQueryHandler.cs` | Fetches one account by id (404 when missing). |
| `CreateUserCommand.cs` / `CreateUserCommandHandler.cs` / `CreateUserCommandValidator.cs` | Hashes the raw password, rejects duplicate usernames (409), validates the primary department and optional employee. |
| `UpdateUserCommand.cs` / `UpdateUserCommandHandler.cs` / `UpdateUserCommandValidator.cs` | Updates department, employee link, scope type, and must-change-password flag. |
| `ResetUserPasswordCommand.cs` / `ResetUserPasswordCommandHandler.cs` / `ResetUserPasswordCommandValidator.cs` | Administrative reset for a forgotten password: hashes the temporary password, sets `MustChangePassword`, and passes the calling administrator into `User.ResetPassword` so the audit row names them (404 `Identity.UserNotFound`). |
| `DeactivateUserCommand.cs` / `DeactivateUserCommandHandler.cs` | Soft delete via `User.Deactivate`. |
| `ActivateUserCommand.cs` / `ActivateUserCommandHandler.cs` | Reactivates a soft-deleted account. |
| `AssignUserRoleCommand.cs` / `AssignUserRoleCommandHandler.cs` / `AssignUserRoleCommandValidator.cs` | Grants a role and records the grantor (409 `Identity.RoleAlreadyAssigned`). |
| `RevokeUserRoleCommand.cs` / `RevokeUserRoleCommandHandler.cs` / `RevokeUserRoleCommandValidator.cs` | Revokes a role (404 `Identity.RoleNotAssigned`). |
| `AssignUserDepartmentScopeCommand.cs` | CQRS command starting a temporal department assignment (department, type, `IncludeSubtree`, start/end dates). |
| `AssignUserDepartmentScopeCommandHandler.cs` | Validates user (404), department exists + active, assignment type lookup active, then `User.AssignDepartmentScope` — which rejects a department already assigned (409 `Identity.DepartmentAlreadyAssigned`). |
| `AssignUserDepartmentScopeCommandValidator.cs` | FluentValidation rules incl. end-date ≥ start-date (`Identity.InvalidDateRange`). |
| `EndUserDepartmentScopeCommand.cs` / `EndUserDepartmentScopeCommandHandler.cs` | Ends an assignment by id (404 `Identity.DepartmentAssignmentNotFound`); the row is kept with its end date for auditing. |
| `GetUserEffectivePermissionsQuery.cs` / `GetUserEffectivePermissionsQueryHandler.cs` | Projects role codes, flattened permission codes, scope type, primary department, and currently-active assignments. |
| `UserEffectivePermissionsResponse.cs` | Read model for the effective-permissions endpoint (mirrors the JWT claims). |
| `UserDepartmentAssignmentResponse.cs` | Read model for one temporal assignment, nested in the effective-permissions response. |

## Conventions / Notes

- `UserId` always comes from the token via the controller — never from the request body.
- The effective-permissions projection deliberately mirrors `Features/Identity/IdentityTokenIssuer`,
  so UI gating and API authorization cannot drift apart.
- Assignments are temporal, not deletable: `EndAssignment` flips `IsActive` and sets `EndDate`.

## Implementation status

| Item | State |
| --- | --- |
| CRUD + activation + role grant/revoke | ✅ |
| `ResetUserPasswordCommand` (admin reset, forced change) | ✅ |
| `ListUsersQuery` (paged + username search) | ✅ |
| `AssignUserDepartmentScopeCommand`, `EndUserDepartmentScopeCommand` | ✅ |
| `GetUserEffectivePermissionsQuery` | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Added `ResetUserPasswordCommand` for administrators to reset a forgotten password: the handler hashes the temporary password and forces a change at the next sign-in, without re-issuing a token. |
| 2026-10-22 | Buffy | Every handler in this slice now passes `IUnitOfWork.UtcNow` into the `User` behavior it calls (create, role grant/revoke, department-scope assign/end, profile update, activation), so a write is stamped with the transaction's instant rather than the system clock. |
| 2026-10-22 | opencode | `ListUsersQuery` now returns `PagedResult<UserResponse>` with `page`, `pageSize`, and a username `search`; `UserRepository.ListPagedAsync` pages in SQL and the handler resolves roles for the requested page only. |
| 2026-10-19 | Buffy | Added the department-scope use cases (`AssignUserDepartmentScopeCommand`, `EndUserDepartmentScopeCommand`) and `GetUserEffectivePermissionsQuery`, plus `User.AssignDepartmentScope` duplicate guard and `User.EndDepartmentScope`; endpoints added to `UsersController`. |
