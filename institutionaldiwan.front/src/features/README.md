# institutionaldiwan.front / src/features — User Interactions & Business Mutations Layer

> **Status:** Module 01 `department-manage/` and `institution-settings-manage/` are implemented,
> alongside Module 02 `auth/`, `user-manage/`, `role-manage/`, and `employee-manage/`. Module 02 audit
> fixes and final integration verification remain in progress. Module 03 `document-create/` creates
> drafts and registered documents, `document-manage/` owns the lifecycle commands (edit, register,
> participant, cancel), `document-access-grant/` owns grant/revoke, `numbering-manage/` owns
> registry administration plus the official-number lifecycle, `movement-routing/` owns the
> routing commands (open, send, receive, complete, reject, return, redirect, close, cancel), and
> `external-organization-manage/` owns correspondent create/update/activate/deactivate, and Module 09
> `audit-manage/` owns the checkpoint and integrity-check commands; attachment writes remain a
> planned frontend slice mapped to the implemented backend API.

## Purpose

`src/features/` encapsulates user-facing actions and state-changing workflows. Every backend CQRS
`Command` (`POST`/`PUT`/`DELETE`/`PATCH`) maps to a slice here — Zod schema, `useMutation`, RFC 7807
error handling, and form/dialog UI.

## Layer & Import Rules (FSD Strict Hierarchy)

`features` sits below `widgets` and above `entities`:

- Allowed: `@/entities/*`, `@/shared/*` (public barrels only).
- Forbidden: `@/app/*`, `@/pages/*`, `@/widgets/*`, sibling `@/features/*`.
- Layout per slice: `ui/`, `model/`, `api/`, `index.ts`.

## Contents — Feature Slices & Backend Command Mapping

| Slice Folder | Backend Endpoints & CQRS Commands | Key Domain Rules & Error Handling | State |
| --- | --- | --- | --- |
| `auth/` | `POST /api/auth/login`, `POST /api/auth/change-password` (+ `register` out of scope) | JWT storage and replacement; `Identity.InvalidCredentials`, `Identity.UserInactive`, `Identity.CurrentPasswordIncorrect`, `Identity.NewPasswordSameAsOld` rendered via the Arabic error catalog | ✅ Implemented (login/logout/guard/change-password) |
| `document-create/` | `POST /api/documents`, `POST /api/documents/drafts` | Mode-aware form: draft or registered with official number + numbering year; `409 Numbering.OfficialNumberTaken` attaches to the official-number field; `Correspondence.TitleEmpty`/`TypeCodeEmpty` are Arabic-validated client-side | ✅ Implemented (Phase 2) |
| `document-manage/` | `PUT /api/documents/{id}`, `POST .../register`, `POST .../participants`, `POST .../cancel` | Draft-only edit/register; registry-allocated number; one target per participant and one external party per document; cancel requires a reason and rejects active movements | ✅ Implemented (Phase 3) |
| `document-access-grant/` | `POST /api/documents/{id}/access-grants`, `DELETE .../access-grants/{grantId}` | `ACCESS_GRANT_MANAGE`; access levels VIEW/DOWNLOAD/FORWARD/MANAGE; picker shows active users only; optional future expiry; grantee label survives search; revoke keeps the record (the DELETE endpoint takes no reason, so the dialog only confirms) | ✅ Implemented (Phase 4) |
| `numbering-manage/` | `/api/numbering/registries` CRUD + activate; `POST .../numbers/issue`, `.../numbers/{numberId}/cancel\|replace`, `.../numbers/department-registration` | `NUMBERING_MANAGE`; draft-only issue (moves to REGISTERED); `Numbering.OfficialNumberTaken` shown in-dialog; cancel/replace require a reason; department registration requires an active department-scoped registry | ✅ Implemented (Phase 5) |
| `department-manage/` | departments create/update (including parent), deactivate/activate | Immutable code/type on edit; self/descendant exclusion; configured central Diwan protection; inactive-parent activation guard | Implemented with manager picker, notes, and external policies. |
| `institution-settings-manage/` | `PUT /api/institution-settings` | Existing singleton only; active root central Diwan, transfer reason on designation change, active numbering lookups | Implemented; failed/missing reads never initialize or overwrite settings. |
| `user-manage/` | users + employees mutations | `USER_MANAGE` / `EMPLOYEE_MANAGE` | ✅ Implemented (users; employee picker only) |
| `employee-manage/` | employees create/update/activate/deactivate | `EMPLOYEE_MANAGE`; department must exist and be valid | ✅ Implemented |
| `role-manage/` | roles create/update/activate + grant/revoke permission | `ROLE_MANAGE`; system roles protected; code immutable after creation | ✅ Implemented |
| `movement-routing/` | `POST /api/movements`; `POST /api/movements/{id}/send\|receive\|complete\|reject\|return\|redirect\|close\|cancel` | `DOCUMENT_EDIT`; draft-only edit fields; opening requires the Central Diwan and a registered document; receive stays blocked until the destination department records a register number (`Numbering.DepartmentRegistrationRequired`); rejection/return require a reason and reroute to the Central Diwan; invalid transitions surface `Movements.InvalidStatusTransition` | ✅ Implemented (Phase 6) |
| `external-organization-manage/` | `POST /api/external-organizations`, `PUT /{id}`, `DELETE /{id}` (deactivate), `POST /{id}/activate` | `DOCUMENT_EDIT`; code immutable after creation; `Correspondence.ExternalOrgCodeAlreadyExists` shown inside the retained dialog; optional text sent as `null`; deactivated rows keep the record but leave the participant picker | ✅ Implemented (Phase 7) |
| `attachment-upload/` | Module 06 `POST /api/documents/{id}/files`, version upload, archive | Magic bytes + SHA-256; MANAGE access; show checksum after upload | ❌ Planned (API live) |
| `audit-manage/` | `POST /api/audit-logs/checkpoints`, `POST .../integrity-checks` | `AUDIT_MANAGE`; integrity failure is **200 + `isValid: false`** rendered in-dialog; empty checkpoint range → Arabic `Audit.NothingToCheckpoint` / `Audit.ChainBroken` | ✅ Implemented (Module 09) |

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
| `department-manage/` | Implemented: create/edit/reparent/manager/notes/external policy and active-status writes. |
| `institution-settings-manage/` | Implemented: explicit singleton update with central Diwan/numbering/responsible-employee fields. |
| `document-create/` | ✅ Implemented (draft + immediate registration dialog with lookup-driven fields) |
| `document-manage/` | ✅ Implemented (Phase 3: draft edit, register from registry, participant insert, cancel with reason) |
| `document-access-grant/` | ✅ Implemented (Phase 4: add-grant dialog over the identity-options user picker, revoke confirmation, grant-key invalidation) |
| `numbering-manage/` | ✅ Implemented (Phase 5: registry create/edit/activate and issue/cancel/replace/department-registration dialogs; Module 04 Phase 2 added the form's live pattern preview and stricter token validation) |
| `movement-routing/` | ✅ Implemented (Phase 6: open dialog with assignee picker, plus one action dialog covering send, receive, complete, reject, return, redirect, close, and cancel). |
| `external-organization-manage/` | ✅ Implemented (Phase 7: create/edit dialog over the `organization-types` lookup, duplicate-code Arabic error, staged deactivate/activate via `ConfirmDialog`). |
| `audit-manage/` | ✅ Implemented (Module 09: checkpoint button/dialog with optional target log, integrity-check dialog with optional range and in-place pass/fail panel). |
| `attachment-upload/` | ❌ Not implemented (backend live; docs ready). |
| Final integration verification | Pending; Module 02 audit fixes still in progress. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-30 | agent | Module 04 Phase 5: `numbering-manage`'s registry form keeps its own department in the scope select after that department is deactivated (disabled, labelled «(موقوف)») instead of rendering a blank select; manager and read-only flows verified in-browser. |
| 2026-10-29 | agent | Module 04 Phase 2: `numbering-manage`'s registry form now previews the next number from the typed template and rejects unknown tokens or a missing `{SEQ}` with Arabic messages; verified in-browser. |
| 2026-10-29 | agent | Module 09: implemented `audit-manage` — checkpoint and integrity-check mutations, their Zod schemas (optional positive ids, range ordering), and the dialogs that keep the pass/fail result in place and map the 404/409 domain errors through the Arabic catalog. |
| 2026-10-29 | agent | Module 03 Phase 7: implemented `external-organization-manage` — create/update mutations plus the staged activate/deactivate pair, a Zod schema mirroring the server's contact-info rule, and the create/edit dialog over the `organization-types` lookup that keeps `Correspondence.ExternalOrgCodeAlreadyExists` inside the open dialog. |
| 2026-10-29 | agent | Module 03 Phase 6: implemented `movement-routing` — the open-movement dialog (type, destination department, assignee, due date, instructions) plus a single action dialog covering send, receive, complete, reject, return, redirect, close, and cancel, each invalidating the whole `movementKeys` prefix and surfacing Arabic domain errors. |
| 2026-10-29 | agent | Module 03 Phase 5: implemented `numbering-manage` — registry create/edit/activate and the official-number lifecycle (issue, cancel with reason, replace with reason, department registration), each invalidating the registry and document number keys, with Arabic domain errors. |
| 2026-10-28 | Qoder | Module 03 Phase 4: implemented `document-access-grant` — add-grant dialog (debounced `identity-options/users` picker, access-level select, optional future expiry, reason) and a destructive revoke confirmation, both invalidating the document grant keys. |
| 2026-10-28 | Qoder | Module 03 Phase 3: implemented `document-manage` — draft metadata edit, registry registration, participant insert (external/department/person), and cancel with a required reason, each invalidating the document detail and registry keys. |
| 2026-10-28 | Qoder | Module 03 Phase 2: implemented `document-create` (shared draft/register schema, two mutations, and the create dialog wired to seeded document lookups and `documentKeys.lists()` invalidation). |
| 2026-10-28 | agent | Record the implemented Module 01 department and institution-settings write slices, their invariants, and the remaining final verification boundary. |
| 2026-10-28 | agent | Extended `auth` with the signed-in password change (schema, mutation, form) and exposed the login response to pages so reset accounts route to `/change-password`. |
| 2026-10-28 | agent | Implemented `employee-manage` for Phase 5: employee request types/schema, create/update/activate mutations, and the department-aware create/edit dialog. |
| 2026-10-28 | agent | Implemented `role-manage` for Phase 4: role request types/schema, create/update/activate/deactivate mutations, and the grouped permission-matrix dialog that saves only the grant/revoke diff. |
| 2026-10-28 | agent | Implemented `user-manage` for Phase 3: Zod request schemas and seven mutations covering create/update, activate/deactivate, role assign/revoke, and department-assignment add/end, plus the create/edit, roles, and access dialogs. |
| 2026-10-27 | agent | Marked `auth/` as implemented (login, logout, permission guard, Arabic error catalog); register and change-password forms remain pending. |
| 2026-10-25 | Buffy | Clarified that features expose handlers/mutations while the mounting widget owns permission-filtered context-menu item definitions. |
| 2026-10-25 | Cursor | Marked movement/attachment/audit feature slices as backend-ready; added `audit-manage` mapping and integrity HTTP 200 semantics. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/features` FSD layer folder and mapped slices to backend CQRS commands and error codes. |
