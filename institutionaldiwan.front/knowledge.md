# Institutional Diwan React Front End — Agent Knowledge

> **Status:** Current as of **2026-10-29**. Backend Modules **01–06** and **09** plus Lookups are
> implemented. Canonical numbering is **01 Organization / 02 Identity / 03 Correspondence**;
> older references to “Identity Module 01” mean the initial auth slice, not the domain module number.
> Module 01 frontend now includes department create/edit/reparent/manager/notes/external policy,
> activate/deactivate, tree/Mermaid/chart/grid views, optional lazy employee branches, paged searchable
> rosters/details/menus, and `/institution-settings` through new settings entity/feature/page slices.
> Module 02 auth/admin code is implemented and verified, including permission-scoped
> `identity-options` reads, temporal department assignments, live session invalidation, and the
> server-enforced forced-password-change gate. Module 03 Correspondence has started: Phase 0 added
> document-facing picker reads on the API, Phase 1 implemented `entities/document` reads plus the Arabic
> Correspondence/Numbering/Movements error catalog, Phase 2 shipped the `/documents` registry with
> document/draft creation and nav activation, verified in-browser (search/filter/sort, empty state,
> draft and registered creation, the Arabic `Numbering.OfficialNumberTaken` field error, and immediate
> registry refresh), and Phase 3 added `features/document-manage` plus the `/documents/:documentId`
> details page (participants, status history, audit timeline, and the draft-edit / register /
> add-participant / cancel transitions gated on status and permission), and Phase 4 added
> `features/document-access-grant` plus the `grants` tab (grant a user an access level with optional
> future expiry, revoke while keeping the record as history, both behind `ACCESS_GRANT_MANAGE`),
> verified in-browser (Arabic required-field/past-expiry validation, immediate grant row with
> grantor/expiry and counts, revoked rows kept as «ملغى» with timestamp + revoker, and a `USER`-role
> caller seeing no tab with the endpoint returning 403). Phase 5 added `entities/numbering-registry`,
> `features/numbering-manage`, `widgets/numbering-registry-table`, the `/numbering` page, and the
> document-details numbers tab (issue, department registration, replace/cancel with reasons, full
> history), verified in-browser with a `USER`-role check (no numbering UI; direct writes 403). Phase 6
> added `entities/movement`, `features/movement-routing`, `widgets/movement-inbox`, the `/movements`
> inbox/outbox page, and the document-details movements tab (open plus the nine routing commands,
> movement step chain, Arabic event log), verified in-browser with a full two-department cycle
> (send → receive → reject with automatic reroute → receive → close) whose queues and timeline
> refreshed at every step. Attachment slices remain pending.
> `npm run build`/`npm run lint` pass and the API was exercised live (login, stale-token rejection,
> department edit, settings, pickers, registry creation).
>
> **Standalone boundary:** this project is not part of the API solution. Server paths below refer to
> the external `InstitutionalDiwan.Server` repository; the front end talks to it only over `/api`
> and (later) `/hubs`.

## As-is vs to-be (read this first)

| Area | As-is (today) | To-be (before feature work) |
| --- | --- | --- |
| Application code | Module 01 department management/hierarchy/settings; Module 02 auth/admin pages; Module 03 document entity reads, Arabic errors, `/documents` registry, create draft/registered, the `/documents/:documentId` details page with participants/status history/timeline plus draft-edit/register/participant/cancel, the permission-gated access-grants tab (grant/revoke), the numbers tab (issue/cancel/replace/department registration/history), `/numbering` (registry grid + department register book), and the `/movements` inbox/outbox page with the document-details movements tab (open/send/receive/complete/reject/return/redirect/close/cancel behind `DOCUMENT_EDIT`); shared/app foundation, employee picker, and lookup catalogs (incl. the document, participant, and movement catalogs) | Module 03 attachments tab; other module pages pending |
| `package.json` | React/shared/auth stack (router, query, zustand, axios, RHF, zod, sonner, Tailwind v4) plus shared UI/Mermaid dependencies | Remaining packages per slice (see [`README.md`](README.md)) |
| `vite.config.ts` proxy | ✅ `@` alias + env-driven `/api` and `/hubs` proxies (`DEV_PROXY_TARGET` → `ASPNETCORE_HTTPS_PORT` → `https://localhost:7126`) | Optional local HTTPS certificate; production uses the reverse proxy ([`DEPLOYMENT.md`](DEPLOYMENT.md)) |
| Dev configuration | ✅ `.env.example` committed; `.env` is git-ignored | — |
| Backend | External API Modules 01–06 and 09 implemented, including `identity-options` picker reads and live JWT session validation | Frontend consumes relative `/api/...` URLs; refresh endpoint counts when Module 03 work starts |
| Modules without API | 07 Archiving, 08 Relations, 10 Notifications | Do **not** call them; domain/lookups exist only |

## Commands

- Install deps: `npm install` (run inside `institutionaldiwan.front/`)
- Install planned feature stack: see [`README.md`](README.md) “Required Packages”
- Dev server: `npm run dev` (stock Vite, normally `http://localhost:5173`; the HTTPS/port convention
  is to-be once the proxy and certificate are configured)
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
- Arabic-first UI: `dir="rtl"` on the document root, `dir="ltr"` on codes, ids, and official numbers.

## Canonical permissions (16)

Must match the external API's `InstitutionalDiwan.Domain/Common/Permissions.cs` exactly. Front-end
source of truth: `src/shared/config/permissions.ts`.

| Code | Typical use |
| --- | --- |
| `DOCUMENT_VIEW` | Read documents, movements inbox/outbox, file list/download/verify, most lookups |
| `DOCUMENT_CREATE` | Create / draft / register documents |
| `DOCUMENT_EDIT` | Update/cancel documents, participants, movements actions, attachment writes |
| `DOCUMENT_DELETE` | Reserved |
| `DOCUMENT_APPROVE` | Reserved |
| `NUMBERING_VIEW` / `NUMBERING_MANAGE` | Registries, official numbers, department register |
| `DEPARTMENT_MANAGE` | Departments + institution settings |
| `ROLE_MANAGE` | Roles + permission catalog |
| `USER_MANAGE` | Users, roles on users, department assignments |
| `EMPLOYEE_MANAGE` | Employees |
| `ACCESS_GRANT_MANAGE` | Per-document access grants |
| `REPORT_VIEW` | Reserved |
| `LOOKUP_MANAGE` | Mutating reference catalogs |
| `AUDIT_VIEW` | Search audit log + list integrity reports |
| `AUDIT_MANAGE` | Create checkpoints + run integrity checks |

## Live Backend Endpoints (external `InstitutionalDiwan.Server`)

The catalog below records the module APIs, including the `identity-options` picker reads. Every route
except `POST /api/auth/login`, `POST /api/auth/register`, and lookup reads marked public by the server
requires a bearer token. Bearer tokens are also bound to the account and its effective active
role/permission set: deactivation, password change, and role or permission changes invalidate an
already-issued token. The permission column describes the route policy.

Demo credentials: `admin` / `12345678` (system scope), `user` / `12345678` (department scope).

### Paged list responses

These endpoints return the shared `PagedResult<T>` envelope:

`GET /api/documents`, `/api/users`, `/api/employees`, `/api/external-organizations`,
`GET /api/identity-options/users`, `GET /api/audit-logs`, `GET /api/audit-logs/integrity-checks`.

```json
{
  "items": [ ... ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 42,
  "totalPages": 3,
  "hasPrevious": false,
  "hasNext": true
}
```

Query parameters (common): `page` (1-based, default 1), `pageSize` (default 20, clamped 1–100),
`search`. Bounded lists (lookups, permissions, roles, departments, registries, per-document grants,
movements, document files) return a plain JSON array, so they accept no server paging params; the
shared data grid still paginates them **client-side** with the same pager and page-size control
required for every table.

### Auth — `/api/auth`

| Method | Route | Permission | Notes |
| --- | --- | --- | --- |
| `POST` | `/api/auth/login` | — (public) | Returns a JWT access token. |
| `POST` | `/api/auth/register` | — (public) | Self-registration. |
| `GET` | `/api/auth/me` | `DOCUMENT_VIEW` | Caller profile. |
| `POST` | `/api/auth/change-password` | `DOCUMENT_VIEW` | Payload `{ oldPassword, newPassword }`; returns a fresh `LoginResponse` (JWT + identity). New password 8–200 chars and different from the old one (`Identity.CurrentPasswordIncorrect`, `Identity.NewPasswordSameAsOld`). |

### Documents — `/api/documents`

| Method | Route | Permission |
| --- | --- | --- |
| `GET` | `/api/documents` | `DOCUMENT_VIEW` | Paged; also `sortBy`, `sortDescending`, `statusId`, `owningDepartmentId`, `directionId`, `documentTypeId`. |
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
`numberingYear`; `409` with `Numbering.OfficialNumberTaken` when the pair already exists.

### Document access grants — `/api/documents/{documentId}/access-grants`

`GET` (list), `POST` (grant), `DELETE /{grantId}` (revoke) — all `ACCESS_GRANT_MANAGE`.

### Attachments (Module 06) — `/api/document-files` and document-scoped aliases

Access is permission **plus** document access level: writes need **MANAGE**, list/verify need
**VIEW**, download needs **DOWNLOAD**. Prefer the document-scoped routes for the UI.

| Method | Route | Permission | Notes |
| --- | --- | --- | --- |
| `POST` | `/api/documents/{documentId}/files` | `DOCUMENT_EDIT` | **Preferred** atomic create: multipart metadata + first version. |
| `GET` | `/api/documents/{documentId}/files` | `DOCUMENT_VIEW` | List files + version history for one document. |
| `POST` | `/api/document-files` | `DOCUMENT_EDIT` | Metadata-only open; still needs a first version upload. |
| `POST` | `/api/document-files/{fileId}/versions` | `DOCUMENT_EDIT` | Multipart `File` (+ optional `ChangeNotes`). |
| `POST` | `/api/document-files/{fileId}/archive` | `DOCUMENT_EDIT` | Soft-archive; versions stay readable. |
| `GET` | `/api/document-files/by-document/{documentId}` | `DOCUMENT_VIEW` | Alias of document-scoped list. |
| `GET` | `/api/document-files/{fileId}/download` | `DOCUMENT_VIEW` | Streams current (or selected) version bytes. |
| `GET` | `/api/document-files/{fileId}/verify` | `DOCUMENT_VIEW` | Recomputes SHA-256; returns recorded vs actual + `isMatch`. |

Upload validation: extension allow-list, size limit, and magic bytes (PDF/PNG/JPEG/TIFF/Office/SVG).
Each version stores a **64-char hex SHA-256**. Max upload ~50 MB (+ multipart overhead).

### Numbering — `/api/documents/{documentId}/numbers`

| Method | Route | Permission |
| --- | --- | --- |
| `GET` | `.../numbers/history` | `NUMBERING_VIEW` |
| `GET` | `.../numbers/registry-entries` | `NUMBERING_VIEW` |
| `POST` | `.../numbers/issue` | `NUMBERING_MANAGE` |
| `POST` | `.../numbers/{documentNumberId}/cancel` | `NUMBERING_MANAGE` |
| `POST` | `.../numbers/{documentNumberId}/replace` | `NUMBERING_MANAGE` |
| `POST` | `.../numbers/department-registration` | `NUMBERING_MANAGE` |

`department-registration` must exist before `receive` on a movement is allowed.

### Numbering registries — `/api/numbering/registries`

`GET` (list), `GET /{registryId}` — `NUMBERING_VIEW`;
`POST`, `PUT /{registryId}`, `DELETE /{registryId}`, `POST /{registryId}/activate` —
`NUMBERING_MANAGE`.

### Department register — `/api/departments/{departmentId}/register`

`GET` — `NUMBERING_VIEW`. Optional numbering-year filter.

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

Inbox/outbox default to the caller's primary department; `?departmentId=` only inside scope
(otherwise `403` / `Movements.DepartmentOutOfScope`).

### Audit (Module 09) — `/api/audit-logs`

| Method | Route | Permission | Notes |
| --- | --- | --- | --- |
| `GET` | `/api/audit-logs` | `AUDIT_VIEW` | Paged search; **each call appends a self-audit View row**. |
| `POST` | `/api/audit-logs/checkpoints` | `AUDIT_MANAGE` | Body: `{ "toAuditLogId": number \| null }`. Verifies chain before sealing. |
| `POST` | `/api/audit-logs/integrity-checks` | `AUDIT_MANAGE` | Body: `{ "startAuditLogId", "endAuditLogId" }` optional. **HTTP 200 even when `isValid: false`.** |
| `GET` | `/api/audit-logs/integrity-checks` | `AUDIT_VIEW` | Newest-first paged integrity reports. |

List filters: `moduleName`, `actionTypeId`, `entityTypeId`, `entityId`, `actorUserId`,
`actorDepartmentId`, `relatedDocumentId`, `correlationId`, `ipAddress`, `fromUtc`, `toUtc`,
`search`, `sortBy` (`occurredAt` \| `module` \| `action` \| `entity`), `sortDescending`.

Wire shapes (camelCase JSON):

```ts
type AuditLogResponse = {
  auditLogId: number;
  occurredAtUtc: string;
  actorUserId: string | null;
  actorDepartmentId: string | null;
  moduleName: string;
  actionTypeId: number;
  entityTypeId: number;
  entityId: string;
  relatedDocumentId: string | null;
  oldValuesJson: string | null;
  newValuesJson: string | null;
  ipAddress: string | null;
  correlationId: string | null;
  previousHash: string; // 64 hex
  eventHash: string;    // 64 hex
};

type AuditIntegrityCheckResponse = {
  checkId: string;
  startAuditLogId: number;
  endAuditLogId: number;
  isValid: boolean;
  firstCorruptedLogId: number | null;
  failureDetails: string | null;
  executedByUserId: string;
  executedAtUtc: string;
};
```

### Departments — `/api/departments`

`GET` (list), `GET /tree`, `GET /{departmentId}` — `DEPARTMENT_MANAGE`;
`POST`, `PUT /{departmentId}`, `PUT /{departmentId}/parent`,
`DELETE /{departmentId}`, `POST /{departmentId}/activate` — `DEPARTMENT_MANAGE`.

Notable codes: `Department.CyclicHierarchy`, `Department.HasActiveChildren`.

### Institution settings — `/api/institution-settings`

`GET` and `PUT` — `DEPARTMENT_MANAGE`.

### Users — `/api/users`

`GET` (paged list), `GET /{userId}`, `POST`, `PUT /{userId}`, `DELETE /{userId}`,
`POST /{userId}/activate`, `POST /{userId}/reset-password` (payload `{ newPassword }`, 8–200 chars;
sets a temporary password and flags `MustChangePassword`, the caller is recorded as the resetting
administrator), `POST /{userId}/roles`, `DELETE /{userId}/roles/{roleId}`,
`POST /{userId}/department-assignments`,
`DELETE /{userId}/department-assignments/{assignmentId}`,
`GET /{userId}/effective-permissions` — all `USER_MANAGE`. The list accepts `page`, `pageSize`,
`search`, `sortBy` (`username` default, `createdAt`, `lastLoginAt`, `isActive`), and
`sortDescending`; every user response includes its `departmentAssignments` history.

### Identity options — `/api/identity-options`

Minimal picker reads; each requires **one** of the listed exact permission policies and never
exposes full contact data, role permissions, or management rights:

- `GET /api/identity-options/departments` → `{ departmentId, code, nameAr, isActive }`;
  `USER_MANAGE` **or** `EMPLOYEE_MANAGE` **or** `DEPARTMENT_MANAGE` **or** `DOCUMENT_CREATE`
  **or** `DOCUMENT_EDIT`.
- `GET /api/identity-options/roles` → `{ roleId, code, nameAr, isActive }` (inactive included);
  `USER_MANAGE` **or** `ROLE_MANAGE`.
- `GET /api/identity-options/employees?page&pageSize&search` and
  `GET /api/identity-options/employees/{employeeId}` → `{ employeeId, fullNameAr, employeeNumber, isActive }`;
  `USER_MANAGE` **or** `DEPARTMENT_MANAGE` **or** `EMPLOYEE_MANAGE`.
- `GET /api/identity-options/users?page&pageSize&search` →
  `{ userId, username, isActive }` (paged envelope; inactive included for labelling);
  `USER_MANAGE` **or** `ACCESS_GRANT_MANAGE` **or** `DOCUMENT_EDIT`. It is the picker for document
  access grants and movement assignees and returns no contact data, roles, or department history.

### Roles & permissions — `/api/roles`, `/api/permissions`

`GET /api/permissions` — `ROLE_MANAGE`. Returns the flat 16-code catalog with `code`, `nameAr`,
`nameEn`, `moduleName`, and `description`; the SPA groups it by `moduleName` through
`permissionModuleLabel`.
Roles: `GET /api/roles` (plain array), `GET /api/roles/{id}`, `POST /api/roles`
(`code`/`nameAr`/`nameEn`/`description`), `PUT /api/roles/{id}` (name/description only — `code` is
immutable), `DELETE /api/roles/{id}` (deactivate), `POST /api/roles/{id}/activate`,
`POST /api/roles/{id}/permissions` (`{ permissionCode }`), and
`DELETE /api/roles/{id}/permissions/{permissionCode}` — all `ROLE_MANAGE`. The matrix dialog sends
sequential grants then revokes and invalidates the role queries in `onSettled`. System roles cannot
be deactivated (`Identity.SystemRoleProtected`); EF limits are code 100, names 150, description 500.

### Employees — `/api/employees`

All endpoints require `EMPLOYEE_MANAGE`.

- `GET /api/employees?page&pageSize&search&departmentId&includeSubDepartments&sortBy&sortDescending`
  — paged result; `search` matches the Arabic name, English name, and employee number;
  `departmentId` restricts the page to that department's employees, and
  `includeSubDepartments=true` widens it to every descendant department (unknown department → empty
  page, not 404). `sortBy` accepts `fullNameAr` (default), `employeeNumber`, `createdAt`, `isActive`.
- `GET /api/employees/{id}` — single employee.
- `POST /api/employees` / `PUT /api/employees/{id}` — body
  `{ fullNameAr, departmentId, employeeNumber?, fullNameEn?, jobTitleAr?, email?, phone? }`;
  `departmentId` must be an existing department (the SPA loads it from `GET /api/departments`,
  which itself needs `DEPARTMENT_MANAGE`). EF limits: names 200, employee number 50, job title 150,
  email 150, phone 50.
- `DELETE /api/employees/{id}` — deactivate; `POST /api/employees/{id}/activate` — reactivate.

Error codes: `Identity.EmployeeNameEmpty`, `Identity.EmployeeDepartmentEmpty`,
`Identity.EmployeeEmailInvalid`, `Identity.EmployeeNotFound`, `Department.NotFound` — all mapped in
`shared/api/errorMessages.ts`.

### External organizations — `/api/external-organizations`

Paged list (`?includeInactive=`), get — `DOCUMENT_VIEW`;
create/update/deactivate/activate — `DOCUMENT_EDIT`.

### Lookups — `/api/lookups/{lookup}`

`GET` (list, `?includeInactive=`), `GET /{lookup}/{id}` — `DOCUMENT_VIEW`;
`POST`, `PUT /{lookup}/{id}`, `PATCH /{lookup}/{id}/deactivate`, `PATCH /{lookup}/{id}/activate` —
`LOOKUP_MANAGE`. Unknown slug → 404.

34 lookup slugs:

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
(e.g. `Numbering.OfficialNumberTaken`, `Department.CyclicHierarchy`, `Audit.NothingToCheckpoint`)
and `detail` is the human message. `shared/api/httpClient.ts` normalizes this into an
`ApiError` exposing `.code` (= `title`), `.detail`, and `.status`.

**The UI is Arabic-only:** the server's `detail` is English, so surfaces render
`toArabicErrorMessage(error)` from `shared/api/errorMessages.ts`, which maps the domain code to an
Arabic message (full Identity set today) with Arabic HTTP-status fallbacks. Extend the catalog per
module as slices land; never render the raw `detail` to the user.

Branch UI on `.code`, never on HTTP status alone. Exception: audit integrity **command** returns
**200** with `isValid: false` — treat as a successful RPC whose payload reports failure.

## Not yet implemented on the backend (front end must not call)

| Module | What exists | What is missing |
| --- | --- | --- |
| 07 — Archiving | Domain entities + lookups + EF config | Application handlers, repositories in use, controllers |
| 08 — Relations | Domain entities + lookups + EF config | Same |
| 10 — Notifications | Domain entities + lookups + EF config | Same; transactional outbox processor empty |
| SignalR hubs | Planned | Not wired in `Program.cs` |
| CORS | — | Not configured (development will rely on the Vite proxy once it is added) |

## UI acceptance (minimum per interactive screen)

Every page/widget that loads server data must define:

1. **Loading** — skeleton or spinner in the content region (not a blank page).
2. **Empty** — Arabic-first empty copy + optional primary CTA when the list is empty by design.
3. **Error** — toast or inline alert showing `toArabicErrorMessage(error)` (never the raw English
   `detail`); retry when safe.
4. **Forbidden** — when the caller lacks the route permission, show a dedicated state (do not loop
   redirect forever).
5. **URL state** — filters, search, sorting, pagination, and tabs live in `useSearchParams`.
6. **Context menus** — every row, header, empty area, tree node, card, and shell region exposes its
   actions on right-click through `shared/ui/context-menu`; items are permission-filtered and the
   same actions open from the keyboard (`Shift+F10` / `ContextMenu`).
7. **Table contract** — all tables (paged endpoints and plain arrays alike) have filters, search,
   sorting, pagination, and a column chooser. Column visibility is saved per table per browser in
   `localStorage` under `diwan.table.<tableId>.columns.v1`, with reset-to-default; it is never sent
   to the server or put in the URL.
8. **Visualization** — Mermaid source renders through the lazy-loaded `shared/ui/mermaid-view`
   (strict security, token-derived `themeVariables`, Arabic-first error state). Hierarchy trees are
   built from API data (`GET /api/departments/tree`; the department's employees are listed in the
   detail dialog via `GET /api/employees?departmentId=&includeSubDepartments=`, while employee
   *nodes* inside the tree remain pending) with the pure
   builder in `entities/department/model/toMermaid.ts` (`flowchart TD`). Flowchart nodes are
   activatable and open the same detail dialog (the builder returns the node-id → department-id
   map; activation goes through `onNodeClick`, never Mermaid `click` directives, which strict
   security strips). The
   `widgets/department-hierarchy-tree` slice owns the tree / Mermaid / table switcher today; the
   cross-module `widgets/visualization-panel` remains planned.
   Invalid Mermaid shows an Arabic-first error state with the parser message.
9. **Arabic/RTL and theme** — every screen is built for Arabic `dir="rtl"` first and verified in
   LTR as well, and in both light and dark themes. Colors come only from
   `src/app/styles/tokens.css`; the user's light/dark/system choice persists in `diwan.theme`.
   Directional icons mirror in RTL; codes, numbers, hashes, and ISO dates stay `dir="ltr"`.

See [`README.md`](README.md) “UI / design contract” and [`src/app/styles/README.md`](src/app/styles/README.md).

## Recommended build order (first vertical slices)

1. `shared/` (`api`, `config/permissions`, `types`, `ui/context-menu`, `ui/data-table`,
   `ui/mermaid-view`, `lib/columnPreferences`) + `vite` proxy + Tailwind entry — ✅ done
2. `features/auth` + `entities/user` + `app/router` + `pages/login` — ✅ done
3. `widgets/app-shell` + `pages/dashboard` — ✅ done (Phase 1)
4. `entities/{department,role,employee,lookup}` + user admin reads — ✅ done (Phase 2)
5. Module 02 admin pages/features: users ✅ (Phase 3) → roles ✅ (Phase 4) → employees ✅ (Phase 5)
   → change-password ✅ (Phase 6) — Identity admin front end complete
6. Module 01 Phase 1: department hierarchy read-only (tree / Mermaid / table) — ✅ done
   (`shared/ui/mermaid-view`, `entities/department` `toMermaid`, `widgets/department-hierarchy-tree`,
   `pages/departments`), plus the detail dialog's employee roster
   (`GET /api/employees?departmentId=&includeSubDepartments=`); write actions and employee nodes
   inside the tree pending
7. `entities/document` — ✅ done (Phase 1); `pages/documents` registry + `document-create` — ✅ done
   (Phase 2); `features/document-manage` + `pages/documents` details (participants, status history,
   timeline, draft-edit/register/participant/cancel) — ✅ done (Phase 3); `features/document-access-grant`
   + the `grants` tab — ✅ done (Phase 4); `entities/numbering-registry` + `features/numbering-manage`
   + `pages/numbering` + the document numbers tab — ✅ done (Phase 5); `entities/movement` +
   `features/movement-routing` + `widgets/movement-inbox` + `pages/movements` + the document movements
   tab — ✅ done (Phase 6); the attachments panel (`entities/document-file`) pending
8. `features/external-organization-manage` (Module 03 Phase 7) — pending
9. `pages/audit` (search + integrity; respect self-audit side effect)
10. `widgets/visualization-panel` (cross-module Mermaid/chart switcher) + department/employee tree extras

## Mandatory Documentation Rule (`AGENTS.md`)

Whenever you add, rename, delete, or modify files in any folder, you MUST update that folder's
`README.md` (`Contents`, `Implementation status`, and `Change Log` tables) in the same turn.
