# institutionaldiwan.front / src/features — User Interactions & Business Mutations Layer

> **Status:** Partially implemented — `auth/` is live; the remaining feature slices are documented and pending.
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
| `auth/` | `POST /api/auth/login` (+ `register` / `change-password` pending) | JWT storage; `Identity.InvalidCredentials` / `Identity.UserInactive` rendered via the Arabic error catalog | ✅ Implemented (login/logout/guard) |
| `document-create/` | `POST /api/documents` (+ drafts/register) | `409 Numbering.OfficialNumberTaken` on official number field | ❌ Planned |
| `document-access-grant/` | grants POST/DELETE | `ACCESS_GRANT_MANAGE`; access levels VIEW/DOWNLOAD/FORWARD/MANAGE | ❌ Planned |
| `department-manage/` | departments CRUD/reparent/activate | `Department.CodeAlreadyExists`, `CyclicHierarchy`, `HasActiveChildren` | ❌ Planned |
| `user-manage/` | users + employees mutations | `USER_MANAGE` / `EMPLOYEE_MANAGE` | ❌ Planned |
| `role-manage/` | roles + grant/revoke permission | `ROLE_MANAGE`; system roles protected | ❌ Planned |
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
| `auth/` | ✅ Implemented (Identity Module 01) |
| `document-create/`, `document-access-grant/`, `department-manage/`, `user-manage/`, `role-manage/` | ❌ Not implemented (backend live) |
| `movement-routing/`, `attachment-upload/`, `audit-manage/` | ❌ Not implemented (backend live; docs ready) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Marked `auth/` as implemented (login, logout, permission guard, Arabic error catalog); register and change-password forms remain pending. |
| 2026-10-25 | Buffy | Clarified that features expose handlers/mutations while the mounting widget owns permission-filtered context-menu item definitions. |
| 2026-10-25 | Cursor | Marked movement/attachment/audit feature slices as backend-ready; added `audit-manage` mapping and integrity HTTP 200 semantics. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/features` FSD layer folder and mapped slices to backend CQRS commands and error codes. |
