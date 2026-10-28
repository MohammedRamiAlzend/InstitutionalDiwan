# institutionaldiwan.front / src/features — User Interactions & Business Mutations Layer

> **Status:** Partially implemented — `auth/`, `user-manage/`, `role-manage/`, and `employee-manage/` are live; the remaining feature slices are documented and pending.
> Movement, attachment, and audit write slices are now mapped to **live** APIs.

## Purpose

`src/features/` encapsulates user-facing actions and state-changing workflows. Every backend CQRS
`Command` (`POST`/`PUT`/`DELETE`/`PATCH`) maps to a slice here — Zod schema, `useMutation`, RFC 7807
error handling, and form/dialog UI.

## Layer & Import Rules (FSD Strict Hierarchy)

`features` sits below `widgets` and above `entities`:

- Allowed: `@/entities/*`, `@/shared/*` (public barrels only).
- Forbidden: `@/app/*`, `@/pages/*`, `@/widgets/*`, sibling `@/features/*`.
- Layout per slice: `ui/`, `model/`, `api/`, `index.ts`.

## Planned Feature Slices & Backend Command Mapping

| Slice Folder | Backend Endpoints & CQRS Commands | Key Domain Rules & Error Handling | State |
| --- | --- | --- | --- |
| `auth/` | `POST /api/auth/login`, `POST /api/auth/change-password` (+ `register` out of scope) | JWT storage and replacement; `Identity.InvalidCredentials`, `Identity.UserInactive`, `Identity.CurrentPasswordIncorrect`, `Identity.NewPasswordSameAsOld` rendered via the Arabic error catalog | ✅ Implemented (login/logout/guard/change-password) |
| `document-create/` | `POST /api/documents` (+ drafts/register) | `409 Numbering.OfficialNumberTaken` on official number field | ❌ Planned |
| `document-access-grant/` | grants POST/DELETE | `ACCESS_GRANT_MANAGE`; access levels VIEW/DOWNLOAD/FORWARD/MANAGE | ❌ Planned |
| `department-manage/` | departments CRUD/reparent/activate | `Department.CodeAlreadyExists`, `CyclicHierarchy`, `HasActiveChildren` | ❌ Planned |
| `user-manage/` | users + employees mutations | `USER_MANAGE` / `EMPLOYEE_MANAGE` | ✅ Implemented (users; employee picker only) |
| `employee-manage/` | employees create/update/activate/deactivate | `EMPLOYEE_MANAGE`; department must exist and be valid | ✅ Implemented |
| `role-manage/` | roles create/update/activate + grant/revoke permission | `ROLE_MANAGE`; system roles protected; code immutable after creation | ✅ Implemented |
| `movement-routing/` | Module 05 send/receive/complete/reject/return/redirect/close/cancel | Central Diwan hub; receive gated on department registration; reasons required on reject/return | ❌ Planned (API live) |
| `attachment-upload/` | Module 06 `POST /api/documents/{id}/files`, version upload, archive | Magic bytes + SHA-256; MANAGE access; show checksum after upload | ❌ Planned (API live) |
| `audit-manage/` | `POST /api/audit-logs/checkpoints`, `POST .../integrity-checks` | `AUDIT_MANAGE`; integrity failure is **200 + `isValid: false`** | ❌ Planned (API live) |

## Conventions

- Map `ProblemDetails.title` → `ApiError.code`; never show a generic failure when a domain code exists.
- Lookup IDs are integers in payloads.
- Invalidate the owning entity query keys on success (never hand-written key strings).
- A feature exposes the mutation/handler; the widget that mounts the surface owns the context-menu
  item definition (label, icon, permission code, confirmation) and calls the feature. No menu
  rendering or keyboard wiring inside a feature slice.

## Implementation status

| Feature Slice | State |
| --- | --- |
| `auth/` | ✅ Implemented (login, logout, permission guard, signed-in password change) |
| `user-manage/` | ✅ Implemented for users (create/edit, status, roles, department assignments); the employee selector is read-only |
| `role-manage/` | ✅ Implemented (create/edit, activate/deactivate, permission matrix with staged grant/revoke diff) |
| `employee-manage/` | ✅ Implemented (create/edit, activate/deactivate, department-aware form) |
| `document-create/`, `document-access-grant/`, `department-manage/` | ❌ Not implemented (backend live) |
| `movement-routing/`, `attachment-upload/`, `audit-manage/` | ❌ Not implemented (backend live; docs ready) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Extended `auth` with the signed-in password change (schema, mutation, form) and exposed the login response to pages so reset accounts route to `/change-password`. |
| 2026-10-28 | agent | Implemented `employee-manage` for Phase 5: employee request types/schema, create/update/activate mutations, and the department-aware create/edit dialog. |
| 2026-10-28 | agent | Implemented `role-manage` for Phase 4: role request types/schema, create/update/activate/deactivate mutations, and the grouped permission-matrix dialog that saves only the grant/revoke diff. |
| 2026-10-28 | agent | Implemented `user-manage` for Phase 3: Zod request schemas and seven mutations covering create/update, activate/deactivate, role assign/revoke, and department-assignment add/end, plus the create/edit, roles, and access dialogs. |
| 2026-10-27 | agent | Marked `auth/` as implemented (login, logout, permission guard, Arabic error catalog); register and change-password forms remain pending. |
| 2026-10-25 | Buffy | Clarified that features expose handlers/mutations while the mounting widget owns permission-filtered context-menu item definitions. |
| 2026-10-25 | Cursor | Marked movement/attachment/audit feature slices as backend-ready; added `audit-manage` mapping and integrity HTTP 200 semantics. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/features` FSD layer folder and mapped slices to backend CQRS commands and error codes. |
