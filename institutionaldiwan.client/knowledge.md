# Institutional Diwan React Client — Freebuff / Codebuff Knowledge

> **Status:** Current as of 2026-10-22 — reflects the backend as of commit `0c5411d`
> (Modules 01–05 complete: Organization, Identity, Correspondence, Numbering, Movements).

## Commands

- Install deps: `npm install` (run inside `institutionaldiwan.client/`)
- Dev server: `npm run dev` (runs Vite on `https://localhost:61325`, proxying `/api` and `/hubs`
  to `https://localhost:7126`)
- Typecheck & build: `npm run build` (`tsc -b && vite build`)
- Lint: `npm run lint` (`eslint .`)

## Architecture: Feature-Sliced Design (FSD v2.1)

- Layers inside `src/` (strict top-to-bottom import rule):
  `app` → `pages` → `widgets` → `features` → `entities` → `shared`.
- Never import upward or across sibling slices on the same layer.
- Every slice and `shared` segment must export only via its root `index.ts` barrel.
- Reads (`GET`) go in `src/entities/<slice>/` (`api/`, `model/` with query key factory & `useQuery`,
  `ui/` badges/cards).
- Writes (`POST`/`PUT`/`DELETE`) go in `src/features/<slice>/` (`api/`, `model/` with Zod schema &
  `useMutation`, `ui/` forms/dialogs).
- Arabic-first UI: `dir="rtl"` on the document root, `dir="ltr"` on code, ids, and official numbers.

## Live Backend Endpoints (`InstitutionalDiwan.Server`)

15 controllers, 95 endpoints. Every route except `api/auth/login`, `api/auth/register`, and
`api/lookups` requires a bearer token; the permission column is the policy that guards it.

### Auth — `/api/auth`

| Method | Route | Permission | Notes |
| --- | --- | --- | --- |
| `POST` | `/api/auth/login` | — (public) | Returns a JWT access token. |
| `POST` | `/api/auth/register` | — (public) | Self-registration. |
| `GET` | `/api/auth/me` | `DOCUMENT_VIEW` | The caller's profile. |
| `POST` | `/api/auth/change-password` | `DOCUMENT_VIEW` | |

Demo dev credentials: `admin` / `12345678` (system scope, `ADMIN` role) and
`user` / `12345678` (department scope).

### Documents — `/api/documents`

| Method | Route | Permission |
| --- | --- | --- |
| `GET` | `/api/documents` | `DOCUMENT_VIEW` |
| `GET` | `/api/documents/{documentId}` | `DOCUMENT_VIEW` |
| `GET` | `/api/documents/{documentId}/details` | `DOCUMENT_VIEW` |
| `GET` | `/api/documents/{documentId}/timeline` | `DOCUMENT_VIEW` |
| `POST` | `/api/documents` | `DOCUMENT_CREATE` |
| `POST` | `/api/documents/drafts` | `DOCUMENT_CREATE` |
| `PUT` | `/api/documents/{documentId}` | `DOCUMENT_EDIT` |
| `POST` | `/api/documents/{documentId}/register` | `DOCUMENT_CREATE` |
| `POST` | `/api/documents/{documentId}/participants` | `DOCUMENT_EDIT` |
| `POST` | `/api/documents/{documentId}/cancel` | `DOCUMENT_EDIT` |

`POST /api/documents` requires `title`, `documentTypeId`, `directionId`, `officialNumberText`, and
`numberingYear`; it answers `409 Conflict` with code `Numbering.OfficialNumberTaken` when the
`(officialNumberText, numberingYear)` pair already exists.

### Document access grants — `/api/documents/{documentId}/access-grants`

`GET` (list), `POST` (grant), `DELETE /{grantId}` (revoke) — all `ACCESS_GRANT_MANAGE`.

### Numbering — `/api/documents/{documentId}/numbers`

| Method | Route | Permission |
| --- | --- | --- |
| `GET` | `.../numbers/history` | `NUMBERING_VIEW` |
| `GET` | `.../numbers/registry-entries` | `NUMBERING_VIEW` |
| `POST` | `.../numbers/issue` | `NUMBERING_MANAGE` |
| `POST` | `.../numbers/{documentNumberId}/cancel` | `NUMBERING_MANAGE` |
| `POST` | `.../numbers/{documentNumberId}/replace` | `NUMBERING_MANAGE` |
| `POST` | `.../numbers/department-registration` | `NUMBERING_MANAGE` |

`department-registration` records the destination department's own register number and date — the
`receive` movement action is refused until it exists.

### Numbering registries — `/api/numbering/registries`

`GET` (list), `GET /{registryId}` — `NUMBERING_VIEW`;
`POST`, `PUT /{registryId}`, `DELETE /{registryId}`, `POST /{registryId}/activate` —
`NUMBERING_MANAGE`.

### Department register — `/api/departments/{departmentId}/register`

`GET` — `NUMBERING_VIEW`. The register book of one department, optionally filtered by numbering
year.

### Movements — `/api/movements`

| Method | Route | Permission |
| --- | --- | --- |
| `GET` | `/api/movements/inbox` | `DOCUMENT_VIEW` |
| `GET` | `/api/movements/outbox` | `DOCUMENT_VIEW` |
| `GET` | `/api/movements/{movementId}` | `DOCUMENT_VIEW` |
| `GET` | `/api/movements/by-document/{documentId}` | `DOCUMENT_VIEW` |
| `POST` | `/api/movements` | `DOCUMENT_EDIT` |
| `POST` | `/api/movements/{movementId}/send` | `DOCUMENT_EDIT` |
| `POST` | `/api/movements/{movementId}/receive` | `DOCUMENT_EDIT` |
| `POST` | `/api/movements/{movementId}/complete` | `DOCUMENT_EDIT` |
| `POST` | `/api/movements/{movementId}/reject` | `DOCUMENT_EDIT` |
| `POST` | `/api/movements/{movementId}/return` | `DOCUMENT_EDIT` |
| `POST` | `/api/movements/{movementId}/redirect` | `DOCUMENT_EDIT` |
| `POST` | `/api/movements/{movementId}/close` | `DOCUMENT_EDIT` |
| `POST` | `/api/movements/{movementId}/cancel` | `DOCUMENT_EDIT` |

`GET /api/movements/inbox` and `/api/movements/outbox` default to the caller's own primary
department and accept `?departmentId=` only inside the caller's organizational scope (403
otherwise).

### Departments — `/api/departments`

`GET` (list), `GET /tree`, `GET /{departmentId}` — `DEPARTMENT_MANAGE`;
`POST`, `PUT /{departmentId}`, `PUT /{departmentId}/parent` (reparent, subtree cycle check →
`409 Department.CyclicHierarchy`), `DELETE /{departmentId}` (soft deactivate →
`409 Department.HasActiveChildren`), `POST /{departmentId}/activate` — `DEPARTMENT_MANAGE`.

### Institution settings — `/api/institution-settings`

`GET` and `PUT` — `DEPARTMENT_MANAGE`. The singleton institution record; the Central Diwan
department is guarded.

### Users — `/api/users`

`GET` (list), `GET /{userId}`, `POST`, `PUT /{userId}`, `DELETE /{userId}`,
`POST /{userId}/activate`, `POST /{userId}/roles`, `DELETE /{userId}/roles/{roleId}`,
`POST /{userId}/department-assignments`,
`DELETE /{userId}/department-assignments/{assignmentId}`,
`GET /{userId}/effective-permissions` — all `USER_MANAGE`.

### Roles & permissions — `/api/roles`, `/api/permissions`

`GET /api/permissions` — `ROLE_MANAGE`.
`GET` (list), `GET /{roleId}`, `POST`, `PUT /{roleId}`, `DELETE /{roleId}`,
`POST /{roleId}/activate`, `POST /{roleId}/permissions`,
`DELETE /{roleId}/permissions/{permissionCode}` — all `ROLE_MANAGE`.

### Employees — `/api/employees`

`GET` (list), `GET /{employeeId}`, `POST`, `PUT /{employeeId}`, `DELETE /{employeeId}`,
`POST /{employeeId}/activate` — all `EMPLOYEE_MANAGE`.

### External organizations — `/api/external-organizations`

`GET` (list, `?includeInactive=`), `GET /{externalOrganizationId}` — `DOCUMENT_VIEW`;
`POST`, `PUT /{externalOrganizationId}`, `DELETE /{externalOrganizationId}` (deactivate),
`POST /{externalOrganizationId}/activate` — `DOCUMENT_EDIT`.

### Lookups — `/api/lookups/{lookup}`

`GET` (list, `?includeInactive=`), `GET /{lookup}/{id}` — `DOCUMENT_VIEW`;
`POST`, `PUT /{lookup}/{id}`, `PATCH /{lookup}/{id}/deactivate`, `PATCH /{lookup}/{id}/activate` —
`LOOKUP_MANAGE`. An unknown slug is a 404.

34 lookup slugs are exposed, one per seeded reference catalog:

`access-levels`, `archive-event-types`, `archive-locations`, `assignment-types`,
`audit-action-types`, `audit-entity-types`, `department-types`,
`document-confidentiality-levels`, `document-directions`, `document-file-types`,
`document-number-event-types`, `document-number-scopes`, `document-number-statuses`,
`document-priority-levels`, `document-relation-types`, `document-status-types`, `document-types`,
`file-storage-providers`, `movement-event-types`, `movement-step-statuses`, `movement-statuses`,
`movement-types`, `notification-channels`, `notification-outbox-statuses`,
`notification-priorities`, `notification-types`, `numbering-modes`, `numbering-reset-policies`,
`organization-types`, `participant-roles`, `participant-target-types`, `relation-action-types`,
`storage-provider-types`, `user-scope-types`.

## Error Shape (RFC 7807)

Every failure returns `{ type, title, status, detail }` where `title` is the domain error code
(e.g. `Numbering.OfficialNumberTaken`, `Department.CyclicHierarchy`) and `detail` is the human
message. `shared/api/httpClient.ts` must normalize this into an `ApiError` exposing
`.code` (= `title`), `.detail`, and `.status`.

## Not Yet Implemented on the Backend

Modules 06–10 have domain entities and seeded lookups but no API yet — the client must not call
them: Attachments (06), Archiving (07), Relations (08), Audit (09), Notifications (10).

## Mandatory Documentation Rule (`AGENT.md`)

Whenever you add, rename, delete, or modify files in any folder, you MUST update that folder's
`README.md` (`Contents`, `Implementation status`, and `Change Log` tables) in the same turn.
