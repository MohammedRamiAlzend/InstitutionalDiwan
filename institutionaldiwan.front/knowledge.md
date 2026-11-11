# Institutional Diwan React Front End — Agent Knowledge

> **Status:** Current as of **2026-10-30**. Backend Modules **01–06** and **09** plus Lookups are
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
> refreshed at every step. Phase 7 added `features/external-organization-manage` plus
> `widgets/external-organizations-table` as the «الجهات الخارجية» tab inside `/documents` (no new
> route or nav entry), verified in-browser (create with the Arabic success toast, a duplicate code
> keeping the dialog open with «توجد جهة خارجية أخرى بنفس الرمز.», edit with the code read-only, the
> participant picker offering the updated correspondent, and deactivation/activation switching
> between «موقوف»/«نشط» while toggling the correspondent's presence in that picker). Phase 8 closed
> the module with a role-matrix pass (`DOC-MGR` / `NUM-MGR` / `GRANT-MGR` roles, one user each):
> navigation, detail chips, and write actions matched each role's permissions in the browser, direct
> calls to missing routes returned 403, and the draft / canceled / replaced-number / movement states
> were re-checked; no code changed in that phase. Attachment slices remain pending. Module 04
> Numbering is complete: Phase 1 added the pure template model (`renderNumberPattern`) and the scope
> helpers plus `DOCUMENT_NUMBER_STATUS`/`DOCUMENT_NUMBER_EVENT_TYPE` constants, Phase 2 added
> `/numbering` tabs (`?tab=registries|register`) with URL-backed status/policy/scope filters,
> duplicate-scope warnings, and a live template preview with token validation, Phase 3 moved the
> department register book onto the shared data grid (inactive departments keep their history), Phase
> 4 moved the document numbers history grid onto it and removed the hardcoded status/event numbers,
> and Phase 5 verified the manager and read-only flows against the live API (a `NUM-VIEW` user sees
> no write UI and gets 403 on direct writes) while fixing the registry form so a deactivated
> department stays visible, disabled, and labelled «(موقوف)»; all four gates pass. Module 09 Audit
> is implemented: `entities/audit-log` (paged logs + integrity reports, Arabic module labels, hash and
> verdict badges), `features/audit-manage` (checkpoint + integrity check), `widgets/audit-log-table`
> (log grid + report list), and `/audit` (`?panel=logs\|integrity`) behind `AUDIT_VIEW` with
> checkpoint/check behind `AUDIT_MANAGE`; verified in-browser for both a manager and a view-only role.
> Module 06 Attachments is complete: `entities/document-file` (list, blob download, checksum verify),
> `features/attachment-upload` (zod pre-flight limits, atomic upload, new version, versions dialog,
> archive dialog, verify dialog with local SHA-256), `widgets/document-attachments-panel` (client-mode
> grid with row menu, filters, column chooser) and the `files` tab on `/documents/:documentId` — with
> no backend change and the per-type limits mirrored in `shared/config/attachmentLimits.ts`. Phase 5
> verified it in-browser in three roles and proved the access-level rule live: a VIEW-only grant lists
> and verifies (200) but is refused download and upload with «لا تملك صلاحية الوصول إلى هذه الوثيقة.»
> (403), while a MANAGE grant on another document makes the same calls succeed; `FileTypeNotFound`,
> `VersionNotFound` and `ChangeNotesTooLong` were reached by direct API calls because the UI prevents
> them, and a permanent fixture document (two-version scan, archived signature, Office attachment)
> stays in the dev database.
> `npm run build`/`npm run lint` pass and the API was exercised live (login, stale-token rejection,
> department edit, settings, pickers, registry creation).
>
> **Standalone boundary:** this project is not part of the API solution. Server paths below refer to
> the external `InstitutionalDiwan.Server` repository; the front end talks to it only over `/api`
> and (later) `/hubs`.

## As-is vs to-be (read this first)

| Area | As-is (today) | To-be (before feature work) |
| --- | --- | --- |
| Application code | Module 01 department management/hierarchy/settings; Module 02 auth/admin pages; Module 03 document entity reads, Arabic errors, `/documents` registry, create draft/registered, the `/documents/:documentId` details page with participants/status history/timeline plus draft-edit/register/participant/cancel, the permission-gated access-grants tab (grant/revoke), the numbers tab (issue/cancel/replace/department registration/history), `/numbering` (tabbed registry grid + department register book, both shared grids with URL-backed filters), the `/movements` inbox/outbox page with the document-details movements tab (open/send/receive/complete/reject/return/redirect/close/cancel behind `DOCUMENT_EDIT`), the `/documents?tab=organizations` external-correspondent admin tab (create/edit/deactivate/activate behind `DOCUMENT_EDIT`), and the Module 04 numbering model/template/scope work, and the Module 06 attachments `files` tab (list, download, atomic upload, versions, archive, checksum verify); shared/app foundation, employee picker, and lookup catalogs (incl. the document, participant, organization, and movement catalogs) | Other module pages pending |
| `package.json` | React/shared/auth stack (router, query, zustand, axios, RHF, zod, sonner, Tailwind v4) plus shared UI/Mermaid dependencies | Remaining packages per slice (see [`README.md`](README.md)) |
| `vite.config.ts` proxy | ✅ `@` alias + env-driven `/api` and `/hubs` proxies (`DEV_PROXY_TARGET` → `ASPNETCORE_HTTPS_PORT` → `https://localhost:7126`) | Optional local HTTPS certificate; production uses the reverse proxy ([`DEPLOYMENT.md`](DEPLOYMENT.md)) |
| Dev configuration | ✅ `.env.example` committed; `.env` is git-ignored | — |
| Backend | External API Modules 01–06 and 09 implemented, including `identity-options` picker reads and live JWT session validation | Frontend consumes relative `/api/...` URLs; refresh endpoint counts when Module 03 work starts |
| Implemented | 07 Archiving | `/archive` search + saved searches, archive/unarchive and history on `/documents/:id` |
| Implemented | 08 Relations | «الروابط» tab on `/documents/:id`: links grid, Mermaid/tree graph, history, add/remove |
| Implemented | 10 Notifications | Header bell, `/notifications` inbox grid and a lazy SignalR client (see "Notifications" below) |

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

## Canonical permissions (23)

Must match the external API's `InstitutionalDiwan.Domain/Common/Permissions.cs` exactly. Front-end
source of truth: `src/shared/config/permissions.ts`.

| Code | Typical use |
| --- | --- |
| `DOCUMENT_VIEW` | Read documents, movements inbox/outbox, file list/download/verify, most lookups |
| `DOCUMENT_CREATE` | Create / draft / register documents |
| `DOCUMENT_EDIT` | Update/cancel documents, participants, movements actions, attachment writes |
| `DOCUMENT_DELETE` | Reserved |
| `DOCUMENT_APPROVE` | Reserved |
| `DOCUMENT_ARCHIVE` | Archive and unarchive documents (seeded for admin only) |
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
| `SYSTEM_ERRORS_VIEW` | The developer's error log: browse, export, set status and notes, purge |
| `PERSON_VIEW` | The persons screens; documents shown stay limited to what the caller may view |
| `PERSON_MANAGE` | Update a person's recorded details |
| `DEPARTMENT_RECORD_VIEW_ALL` | Read other departments' records on a document (seeded for admin) |
| `DEPARTMENT_RECORD_CORRECT` | Correct a locked department record with a reason (seeded for admin) |
| `DECISION_MANAGE` | Issue a decision, attach its copy, and cancel a mistaken entry (seeded for admin and the standard diwan operator) |

## Live Backend Endpoints (external `InstitutionalDiwan.Server`)

**Table export** (any signed-in user): `POST /api/exports/table` with `{ title, headers[], rows[][] }` (at most 100 columns and 5000 rows, one value per header) returns an `.xlsx`. It reads no stored data. Errors: `Exports.Invalid`, `Exports.TooManyRows`.

**Persons** (`PERSON_VIEW`, limited to documents the caller may view): `GET /api/persons` (paged; `search`, `sortBy`: `name`, `documentCount`, `lastActivity`), `GET /api/persons/{id}`, `GET /api/persons/{id}/documents` (`roleId`, `statusId`, `search`, `sortBy`: `title`, `status`, `linkedAt`), `GET /api/persons/{id}/timeline`. `PUT /api/persons/{id}` needs `PERSON_MANAGE`. `GET /api/persons/suggestions?q=` needs `DOCUMENT_CREATE` or `DOCUMENT_EDIT`, searches everyone, and returns only name, birth year, last four national-number digits and date added. Per document: `GET /api/documents/{id}/persons` (`DOCUMENT_VIEW`), `POST`, `PUT /{documentPersonId}`, `DELETE /{documentPersonId}` (`DOCUMENT_EDIT`; 409 when archived or canceled). `POST /api/documents` and `/drafts` accept `persons: [{ personId? | fullName?, roleId? }]`.

**Document names:** `title` in document responses is written by the system (type and owning department register number, or type and creation date for a draft). The interface neither asks for nor shows a title or summary; `title` and `subjectSummary` stay optional on create. Documents have no document date or external reference; define those as dynamic fields when needed.

**Department records** (each department's record on its visits of a document; plan `plans/department-records.md`): when a department has a published record form for the document type (`GET /api/document-types/{id}/schema?departmentId=`, `DOCUMENT_VIEW`), `POST /api/movements/{id}/receive` (and the `steps/{stepId}/receive` alias) accepts `fieldValues: [{ fieldKey, value }]` and opens the record on the visit; a missing required field fails the whole receipt (400 `DocumentFields.ValueRequired`). `GET /api/documents/{id}/department-records` (`DOCUMENT_VIEW`) returns each register line with its visits (`isLocked`, `versionNumber`, `canEdit`, `canCorrect`, `fields[{ field, value }]`), only for departments in the caller's scope or with `DEPARTMENT_RECORD_VIEW_ALL` + VIEW access. `PUT /api/registry-visits/{visitId}/fields` (`DOCUMENT_EDIT`, own department, open record) replaces the values; `PUT /api/registry-visits/{visitId}/fields/correct` (`DEPARTMENT_RECORD_CORRECT`) corrects a locked one with `{ values, reason }`. A record locks when the document leaves the department (send, redirect, reject, return), when another department receives it, and on archive or cancel. A return to sender gives the sender a new visit. Errors: `DepartmentRecords.VisitLocked`, `VisitNotLocked`, `NotOwnerDepartment`, `SchemaNotFound`, `VisitNotFound`, `CorrectionReasonRequired`, `CorrectionReasonTooLong`.

**Decisions** (plan `plans/case-decisions.md`, slice B). Types: `GET /api/decision-types` (`DOCUMENT_VIEW`; optional `documentTypeId`, `isActive`), `POST /api/decision-types`, `PUT /api/decision-types/{id}`, `POST .../activate` and `.../deactivate` (`LOOKUP_MANAGE`). A referral without a destination is stored inactive; activating it then returns 409 `Decisions.TypeDestinationRequired`. A destination on any other effect is 409 `Decisions.TypeDestinationNotAllowed`. A type has one Arabic name. Editing a type does not change decisions already issued.

On a document (`DOCUMENT_VIEW`, and view access): `GET /api/documents/{id}/decisions` (newest decision first; each decision includes its appeals, newest appeal date first; a joined file also includes the principal file's decisions when the caller may view that file), `GET /api/documents/{id}/next-action`, and `GET /api/documents/{id}/join-candidates`. `kind` is `refer`, `move`, `send-for-archive`, `archive`, or null. `move` is an ordinary referral toward the department that holds the principal file, and only when those departments differ and the caller may view the principal file. The step is a suggestion: the existing movement and archive commands still perform it. A referral wins over the archive route. Nothing is suggested while the file is in transit, archived, or canceled. An appeal does not block movement and does not create one. Join candidates are other unarchived, uncanceled documents of the same type that share a person and that the caller may view. The list is a suggestion, not a join.

`POST /api/documents/{id}/decisions` and `PUT /api/decisions/{id}/file` need `DECISION_MANAGE` and an unlocked department-register visit in the caller's scope (`Decisions.NotHoldingDepartment` otherwise). A non-appealable decision is born final (status 3); an appealable one is born issued (status 1). One decision in force per document (`Decisions.ActiveDecisionExists`). The number is unique per issuing department and year, including revoked and canceled rows (`Decisions.DecisionNumberTaken`). Effect 3 (join) requires `joinTargetDocumentId`. The decision is stored on the joined file and creates one asymmetric `JoinedInto` relation (type 5, «موحَّد مع») from that file to the principal file. It does not change the joined file's number, status, or place, and it is not the principal file's in-force decision. A missing, unseen, archived, canceled, self, already-joined, or cyclic principal file is 409 `Decisions.JoinTargetInvalid`, as is a join target on a decision whose effect is not join. A later separating decision on the joined file is 409 `Decisions.DocumentJoined`. Canceling the join decision removes the relation in the same save. An optional file must be an unarchived decision-type file on the same document.

`POST /api/decisions/{id}/cancel` needs `DECISION_MANAGE` and a system-scoped administrator (`Decisions.NotSystemAdmin` otherwise) plus a reason. `GET /api/decisions` is paged (`search`, `sortBy`: `decisionDate`, `decisionNumber`, `status`, `type`, filters `decisionTypeId`, `statusId`, `issuingDepartmentId`, `year`) and hides decisions on documents the caller cannot view.

`POST /api/decisions/{id}/appeals` and `PUT /api/decisions/{id}/appeals/{appealId}/outcome` need `DECISION_MANAGE` only (no new permission). Either department is enough: the caller's scope covers the issuing department or the appellant department. The document does not have to be held, and no open register visit is required. Both calls are allowed on an archived document; only the decision changes. There is no deadline. An appeal is allowed only when the snapshot `isAppealable` is true and the status is issued (1); otherwise 409 `Decisions.NotAppealable`. A second appeal while one is pending is 409 `Decisions.AppealAlreadyPending`. Recording the appeal sets the status to appealed (2) and the outcome to pending (1). A rejected outcome (3) sets the decision to final (3), still in force. An accepted outcome (2) sets it to revoked (4, «مفسوخ»), which is not in force, so a new decision can be issued. Any other outcome, or an outcome on an appeal that is not pending, is 400 `Decisions.AppealOutcomeInvalid`. A missing appeal is 404 `Decisions.AppealNotFound`. A caller whose scope covers neither department gets 403 `Decisions.AppealOutOfScope`. The appellant defaults to the document's owning department; an explicit department that does not exist is 404 `Department.NotFound`. Appeal outcomes are code constants, not a lookup table: 1 pending, 2 accepted, 3 rejected. No notification is sent for an appeal.

Reopening a document revokes an in-force send-to-archive decision (status 4) with the reopen reason. Other effects stay. Statuses: 1 issued, 2 appealed, 3 final, 4 revoked, 5 canceled. The seeded types, all linked to document type 1, are التخلي and الاتهام (referral, inactive), الظن (referral, appealable, active with destination ديوان الفرد only in Standard mode), منع المحاكمة (send to archive, not appealable, active), التوحيد (join, active). Issuing one notifies the owning department (`NotificationType` 12) and not the actor. Recording an appeal does not.

The «القرار» tab on `/documents/:id` appears when the document type has an active decision type. Unification is one of the types that can be issued. Decisions that belong to the principal file are marked «من الملف الرئيسي» and have no write actions. `/decisions` has the register (`?tab=register`) and, for `LOOKUP_MANAGE`, the types (`?tab=types`). The manual relation dialog does not offer type «موحَّد مع».

**Closing belongs to the creating department:** archive, unarchive and cancel answer 403 `Correspondence.OnlyOwningDepartmentCanClose` unless the caller's department scope covers the document's `owningDepartmentId` (institution-wide scope passes). The details page hides those buttons otherwise.

**Register number filter:** `GET /api/documents?registerNumber=` keeps documents with a line in any department register whose number contains the value.

**Suspended in the interface** (flags in `shared/config/features.ts`): external organizations and external correspondence on departments (`externalOrganizations`), person participants (`personParticipants`), the whole participants tab (`participants`) and saving new drafts (`drafts`). The API and stored rows are unchanged.

**System errors** (`SYSTEM_ERRORS_VIEW`): `GET /api/system-errors` (paged; `search`, `sortBy` lastSeenAt/firstSeenAt/occurrenceCount/title/status, `status`, `source`, `kind`, `lastSeenFromUtc`, `lastSeenToUtc`), `GET /api/system-errors/export` (same filters, `.xlsx`, 10000 rows), `GET`/`PUT /api/system-errors/{id}` (`status` 1 new, 2 in progress, 3 resolved; `notes`), `POST /api/system-errors/purge` (`olderThanDays` 1-3650, returns the count). `POST /api/system-errors/client-reports` is open to any signed-in user (`kind` 3 render, 4 script, 5 network).

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
| `GET` | `/api/auth/me` | `DOCUMENT_VIEW` | Caller profile, including `accessibleDepartmentIds` (the departments the caller's live scope reaches, `null` for an institution-wide scope) and `primaryDepartmentId` (the caller's own department). |
| `POST` | `/api/auth/change-password` | `DOCUMENT_VIEW` | Payload `{ oldPassword, newPassword }`; returns a fresh `LoginResponse` (JWT + identity). New password 8–200 chars and different from the old one (`Identity.CurrentPasswordIncorrect`, `Identity.NewPasswordSameAsOld`). |

### Documents — `/api/documents`

| Method | Route | Permission |
| --- | --- | --- |
| `GET` | `/api/documents` | `DOCUMENT_VIEW` | Paged; also `sortBy`, `sortDescending`, `statusId`, `owningDepartmentId`, `directionId`, `documentTypeId`, repeated `fieldFilters` (see Dynamic document fields). Returns and counts only documents with `VIEW` access. |
| `GET` | `/api/documents/{documentId}` | `DOCUMENT_VIEW` | + `VIEW` access; otherwise 403 `Correspondence.DocumentAccessDenied`. |
| `GET` | `/api/documents/{documentId}/details` | `DOCUMENT_VIEW` | + `VIEW` access. |
| `GET` | `/api/documents/{documentId}/timeline` | `DOCUMENT_VIEW` | + `VIEW` access. |
| `POST` | `/api/documents` | `DOCUMENT_CREATE` |
| `POST` | `/api/documents/drafts` | `DOCUMENT_CREATE` |
| `PUT` | `/api/documents/{documentId}` | `DOCUMENT_EDIT` | + `DOWNLOAD` access; otherwise 403 `Correspondence.DocumentAccessDenied`. |
| `POST` | `/api/documents/{documentId}/register` | `DOCUMENT_CREATE` | + `DOWNLOAD` access. |
| `POST` | `/api/documents/{documentId}/participants` | `DOCUMENT_EDIT` | + `DOWNLOAD` access. |
| `POST` | `/api/documents/{documentId}/cancel` | `DOCUMENT_EDIT` | + `MANAGE` access: the author, a `MANAGE` grant, or an institution-wide scope. |

`POST /api/documents` requires `title`, `documentTypeId`, `officialNumberText`, and
`numberingYear`; `409` with `Numbering.OfficialNumberTaken` when the pair already exists. `directionId`, `confidentialityLevelId` and `priorityLevelId` are suspended: optional on both create endpoints, stored as null and returned as null (null confidentiality counts as normal; stored SECRET/TOP_SECRET values still restrict access).

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
| `GET` | `.../numbers/history` | `NUMBERING_VIEW` | + `VIEW` access. |
| `GET` | `.../numbers/registry-entries` | `NUMBERING_VIEW` | + `VIEW` access. |
| `POST` | `.../numbers/issue` | `NUMBERING_MANAGE` | + `DOWNLOAD` access. |
| `POST` | `.../numbers/{documentNumberId}/cancel` | `NUMBERING_MANAGE` | + `DOWNLOAD` access. |
| `POST` | `.../numbers/{documentNumberId}/replace` | `NUMBERING_MANAGE` | + `DOWNLOAD` access. |
| `POST` | `.../numbers/department-registration` | `NUMBERING_MANAGE` | Body `{ departmentId, registryNumberText, registryDate, notes?, registryId? }`. `departmentId` must be the caller's own primary department (else 403 `Numbering.DepartmentRegistrationOwnDepartmentOnly`); `registryId` picks one of that department's active registers (404 `Numbering.RegistryNotFound`, 409 `Numbering.DepartmentRegistryMismatch`), omitted = best match. |

A department has one number in use per document per year (`DepartmentNumberLocked` if a different one is sent); each arrival adds a dated entry in `visits[]` on `registry-entries`, and a repeated date returns 409 `Numbering.VisitDateDuplicate`. `receive` writes the visit itself, so `department-registration` is only needed to register without receiving.

**Renumbering on return**: `receive` with `issueNewNumber: true` and a new `registryNumberText` writes the returning document under a new department number. The earlier line stays with `statusId` 3 (replaced) and the two are linked: on `registry-entries` each department line has `replacedByNumberId`/`replacedByNumberText` (old → new) and `replacesNumberId`/`replacesNumberText` (new → old); `GET /api/departments/{id}/register` lines have `replacedByNumberText`/`replacesNumberText`; the receipt returns `replacedNumberId`/`replacedNumberText`. It can be repeated (a chain), needs no reason and no extra permission, and the replaced number cannot be reused in that register and year (409 `Numbering.RegistryNumberTaken`). The documents register-number filter matches replaced numbers too.

### Numbering registries — `/api/numbering/registries`

`GET` (list), `GET /{registryId}` — `NUMBERING_VIEW`;
`POST`, `PUT /{registryId}`, `DELETE /{registryId}`, `POST /{registryId}/activate` —
`NUMBERING_MANAGE`.

### Department register — `/api/departments/{departmentId}/register`

`GET` — `NUMBERING_VIEW`. Optional numbering-year filter.

### Movements — `/api/movements`

| Method | Route | Permission |
| --- | --- | --- |
| `GET` | `/api/movements/inbox` | `DOCUMENT_VIEW` | Rows without `VIEW` access have an empty title and no number or instructions. |
| `GET` | `/api/movements/outbox` | `DOCUMENT_VIEW` | Same as the inbox. |
| `GET` | `/api/movements/{movementId}` | `DOCUMENT_VIEW` | + `VIEW` access to the document. |
| `GET` | `/api/movements/by-document/{documentId}` | `DOCUMENT_VIEW` | + `VIEW` access. |
| `POST` | `/api/movements` | `DOCUMENT_EDIT` | + `DOWNLOAD` access. |
| `POST` | `/api/movements/{movementId}/send` | `DOCUMENT_EDIT` | Custody: scope covers the current step's sender; otherwise 403 `Movements.DepartmentOutOfScope`. |
| `POST` | `/api/movements/{movementId}/receive` | `DOCUMENT_EDIT` | Custody: the current step's receiver. Body `{ notes?, registryDate?, registryNumberText?, registryId?, issueNewNumber? }`; `registryId` picks the register for a new number; the number is required only when the department has none for the document in that year (409 `Numbering.DepartmentRegistrationRequired`). `issueNewNumber` replaces the department's number for that year with `registryNumberText` and links the two. |
| `POST` | `/api/movements/{movementId}/complete` | `DOCUMENT_EDIT` | Custody: the receiver. |
| `POST` | `/api/movements/{movementId}/reject` | `DOCUMENT_EDIT` | Custody: the receiver. |
| `POST` | `/api/movements/{movementId}/return` | `DOCUMENT_EDIT` | Custody: the receiver. |
| `POST` | `/api/movements/{movementId}/redirect` | `DOCUMENT_EDIT` | Custody: the receiver, or the sender while the movement is still a draft. Body also takes optional `movementTypeId`: the type the movement carries from this hop on (`5` = to archive); omitted keeps the current type. |
| `POST` | `/api/movements/{movementId}/close` | `DOCUMENT_EDIT` | Custody: either side. |
| `POST` | `/api/movements/{movementId}/cancel` | `DOCUMENT_EDIT` | Custody: the sender. |

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

`GET` and `PUT` — `DEPARTMENT_MANAGE`. Both carry `archiveDiwanDepartmentId` (GUID or `null`): the
archive diwan, an active department other than the Central Diwan. `PUT` replaces the settings, so
omitting the field or sending `null` clears it. Errors: `InstitutionSetting.ArchiveDiwanNotFound`
(400), `ArchiveDiwanInactive` and `ArchiveDiwanSameAsCentral` (409). A designated archive diwan
cannot be deactivated (`Department.ArchiveDiwanProtected`, 409).

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
- `GET /api/identity-options/users/by-ids?ids=…&ids=…` → `{ userId, username, isActive }[]` (at most 100 ids, unknown ids omitted); `USER_MANAGE` **or** `ACCESS_GRANT_MANAGE` **or** `DOCUMENT_EDIT` **or** `DOCUMENT_VIEW` **or** `AUDIT_VIEW`. The front end batches every user label on a page into one call (`UserLabel`).

### Roles & permissions — `/api/roles`, `/api/permissions`

`GET /api/permissions` — `ROLE_MANAGE`. Returns the flat 23-code catalog with `code`, `nameAr`,
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

Paged list (`page`, `pageSize`, `search`, `?includeInactive=`), get — `DOCUMENT_VIEW`;
`POST`, `PUT /{id}`, `DELETE /{id}` (deactivate), `POST /{id}/activate` — `DOCUMENT_EDIT`.

The list accepts no sort parameters, so the SPA grid keeps the server's code ordering and its
headers are not sortable. The code is immutable after creation; `organizationTypeId` uses the
`organization-types` lookup (Government 1 / Private 2 / International 3 / Individual 4). A duplicate
code answers `Correspondence.ExternalOrgCodeAlreadyExists` (Arabic
«توجد جهة خارجية أخرى بنفس الرمز.») and the create dialog stays open. Deactivation is soft: the row
remains in the catalog (visible with `includeInactive`) but leaves the active-only participant
picker in `features/document-manage`. The UI is the `/documents?tab=organizations` tab
(`features/external-organization-manage` + `widgets/external-organizations-table`); there is no
dedicated route or sidebar entry.

### Dynamic document fields — implemented in the SPA (`/document-schemas`, create dialog, «الحقول» tab)

> **Department record forms.** Every schema route above (`fields`, `schema-versions`, `schema`)
> takes an optional `?departmentId=` (and `POST …/fields` an optional `departmentId` in the body):
> it selects that department's **record form** for the type instead of the creation form. Versions,
> drafts and publishing are per form; publishing a department form never retires the creation form.
> See «Department records» below.

Administrators define extra fields per document type and publish them in immutable schema versions
(draft → published → retired). A document is pinned to the version published when it was created
and never moves to a newer one; its values are editable only while it is a draft.

Schema administration — `LOOKUP_MANAGE`:
- `GET`/`POST /api/document-types/{documentTypeId}/fields` — list / define a field
  (`key` lowercase letters, digits, `_`; `dataType` integer 1 Text, 2 LongText, 3 Number, 4 Date,
  5 Boolean, 6 Select — immutable).
- `GET`/`POST /api/document-types/{documentTypeId}/schema-versions` — history (newest first) /
  start the single draft (copied from the published version).
- `GET /api/schema-versions/{id}`, `DELETE /api/schema-versions/{id}` (draft only).
- `POST /api/schema-versions/{id}/fields` (`documentFieldId`, `settings`),
  `PUT`/`DELETE /api/schema-versions/{id}/fields/{documentFieldId}` — edit the draft.
- `POST /api/schema-versions/{id}/publish` — freezes the draft and retires the previous version.

Settings: `labelAr`, `isRequired`, `displayOrder`, `regexPattern` + `regexErrorMessageAr`
(text), `minNumber`/`maxNumber` (number), `maxLength` (text), `options[{key,labelAr}]` (select).

Document side:
- `GET /api/document-types/{documentTypeId}/schema` — `DOCUMENT_VIEW`; the published schema that
  renders the create form (empty when the type has none).
- `GET /api/documents/{id}/fields` — `DOCUMENT_VIEW` + `VIEW` access; the pinned schema with the stored values.
- `PUT /api/documents/{id}/fields` — `DOCUMENT_EDIT` + `DOWNLOAD` access; replaces a draft's values
  (`values[{fieldKey,value}]`, value as text: numbers invariant, dates ISO 8601, booleans
  `true`/`false`, selects the option key; blank clears).
- `POST /api/documents` and `POST /api/documents/drafts` accept optional `fieldValues` (same shape).
- Registering (or `POST /api/documents`) with a required field missing answers
  `DocumentFields.ValueRequired` before any number is consumed.

Error codes:
`DocumentFields.*` — see `InstitutionalDiwan.Domain/Errors/DomainErrors.cs`; all 65 have Arabic entries in
`shared/api/errorMessages.ts`. Error bodies carry no structured field key, so the SPA validates values locally
first. Registry filtering: `GET /api/documents?documentTypeId=N&fieldFilters=key:op:value` (repeat up to 5, AND; requires `documentTypeId`). Operators: text/long text `eq`, `contains`; number and date `eq`, `gt`, `gte`, `lt`, `lte` (dates compare by calendar day, UTC); boolean and select `eq`. Documents without a value for the field never match. Errors: `DocumentFields.FilterRequiresDocumentType`, `FilterMalformed`, `FilterFieldNotFound`, `FilterOperatorNotSupported`, `FilterValueInvalid`, `TooManyFilters` (all with Arabic entries). The SPA keeps the filters in the repeated `f` URL parameter on `/documents`. The department register does not accept them yet.

**Field behavior** (`settings.behavior`, `DocumentFieldBehavior`, stored as JSON on the schema-version field): `defaultValue` (`@today` on dates), `isReadOnly` (needs a default; the server writes it and answers `DocumentFields.FieldReadOnly` for another value), `isFilterable`, `showInTable`, `placeholderAr`, `helpTextAr`, `minLength`, `minDate`/`maxDate` (ISO or `@today`), `visibleWhenFieldKey`/`visibleWhenValue` (boolean, select or multi-select controller; a hidden field is neither required nor stored), `isHalfWidth`, `sectionTitleAr`, `optionSource` (0 static, 1 departments, 2 external organizations, 3 lookup with `lookupCatalog`; the schema reads return the resolved `options`, inactive entries flagged `isActive: false`). Data type 7 `MultiSelect` exchanges option keys joined by commas; its `eq` filter means "among the choices". `fieldFilters` also works on `GET /api/departments/{id}/documents` and `GET /api/archive/documents`. With `documentTypeId`, `GET /api/documents` accepts `sortBy=field:<key>` (text, number, date, boolean) and returns `fieldValues` for the columns marked `showInTable`.

### Archiving (Module 07) — implemented in the SPA (`/archive`, document details)

- `POST /api/documents/{id}/archive` — `DOCUMENT_ARCHIVE` + `MANAGE` access. Body `archiveLocationId`
  (from `/api/lookups/archive-locations`), optional `reason` (max 500). Only `REGISTERED` documents.
- `POST /api/documents/{id}/unarchive` — `DOCUMENT_ARCHIVE` + `MANAGE` access. Body `reason` (required, max 500).
- `GET /api/documents/{id}/archive-events` — `DOCUMENT_VIEW` + `VIEW` access; newest first.
- `GET /api/archive/documents` — `DOCUMENT_VIEW`; paged; `search`, `sortBy` (`archivedAt`, `title`,
  `officialNumber`), `sortDescending`, `owningDepartmentId`, `documentTypeId`, `directionId`,
  `archiveLocationId`, `archivedFromUtc`, `archivedToUtc`. Rows are `{ document, archivedAt,
  archivedByUserId, archiveLocationId }`. Results and counters include only documents the caller may view.
- `GET`/`POST /api/saved-searches`, `PUT`/`DELETE /api/saved-searches/{id}` — `DOCUMENT_VIEW`, owner
  only (another user's search answers 404). Body `name` (max 150), `criteriaJson` (a JSON object string
  with the search parameters above, max 4000), `isDefault`. The server stores the criteria but does not
  run them: read them back and call the archive search. Limit 50 per user, one default.

**Archive diwan (route to the archive).** When `archiveDiwanDepartmentId` is set in the institution
settings, archiving moves to that department and the rules above change:

- A document is sent for archiving in a movement of type `5` («للحفظ»), opened with
  `POST /api/movements` or set on a hop with `redirect`'s `movementTypeId`. A hop that is not the last
  one goes to the creating department or to the Central Diwan: 409 `Archiving.ArchiveRouteViaOwningDepartment`.
  The last hop, to the archive diwan, is sent by the creating department, or by the Central Diwan once
  the movement has already visited the creating department: 409 `Archiving.OnlyOwningDepartmentSendsToArchive`.
  The archive diwan receives
  no other movement type: 409 `Archiving.ArchiveDiwanAcceptsArchiveMovementsOnly`. Type `5` with no
  archive diwan: 409 `Archiving.ArchiveDiwanNotConfigured`.
- `archive` needs the document received at the archive diwan (409 `Archiving.DocumentNotAtArchiveDiwan`)
  and a caller whose scope covers it (403 `Archiving.OnlyArchiveDiwanCanArchive`); `MANAGE` access and
  the owning-department rule do not apply. It closes the movement that brought the document, so an
  `IN_MOVEMENT` document held there can be archived directly.
- `unarchive` of a document the archive diwan holds belongs to the archive diwan (same 403); documents
  archived where they were created keep the owning-department rule. The same reason revokes an in-force
  decision whose effect is send-to-archive.
- The archive diwan may view and download what it holds, also after the movement is closed.
- `GET /api/documents/{id}/details` adds `archiveDiwanDepartmentId` and `isHeldByArchiveDiwan`, which
  the details page uses to offer the two actions.

An archived document rejects participant, attachment, and number writes with 409
`Correspondence.DocumentArchived` until it is unarchived; reads, downloads, checksum verification, and
access grants still work. Any write that loses a concurrent race can now answer 409
`Common.ConcurrencyConflict`: reload and retry.

Error codes (Arabic entries exist in `shared/api/errorMessages.ts`):
`Archiving.*`, `Correspondence.OnlyRegisteredArchivable`, `Correspondence.DocumentArchived`,
`Common.ConcurrencyConflict`.

### Relations (Module 08)

- `GET /api/documents/{id}/relations` — `DOCUMENT_VIEW` + `VIEW` access. `{ items, hiddenRelationCount }`; each item
  has `direction` (`Outgoing`, `Incoming`, `Symmetric`), `type` (`isSymmetric`, `preventCycles`) and a
  `relatedDocument` summary. Links to documents the caller cannot view are only counted.
- `GET /api/documents/{id}/relations/graph?depth=` — same access; depth 1–3 (default 2), at most 100 documents,
  `{ rootDocumentId, nodes, edges, types, hiddenRelationCount, truncated }`. Never passes through a hidden document.
- `GET /api/documents/{id}/relations/events?page=&pageSize=` — paged history, newest first, including removed links.
- `POST /api/documents/{id}/relations` — `DOCUMENT_EDIT` + `DOWNLOAD` on this document and `VIEW` on the target. Body
  `targetDocumentId`, `relationTypeId` (from `/api/lookups/document-relation-types`: 1 reply to, 2 related, 3 supersedes,
  4 duplicate of), optional `notes` (max 500). The first document may not be archived; neither may be cancelled.
- `DELETE /api/documents/{id}/relations/{relationId}` — same permissions. JSON body `{ "reason": "..." }`
  (required, max 500). The removal stays in the history.

Error codes: `Relations.*` (`SelfRelation`, `RelationTypeNotFound`, `RelationTypeInactive`, `DuplicateRelation`,
`CycleDetected`, `DocumentCancelled`, `RelationNotFound`, `ReasonRequired`, `ReasonTooLong`, `NotesTooLong`,
`GraphDepthInvalid`), plus `Correspondence.DocumentArchived`. All twelve `Relations.*` codes have Arabic entries in `errorMessages.ts` (Module 08 section).

### Create and register a document (system number)

`POST /api/documents` body: `title`, `documentTypeId`, `departmentRegistryNumberText`, `departmentRegistryDate` (required,
written into the caller's department register; `409 Numbering.DepartmentRegistryNotConfigured` if none), optional `movement`
(`destinationDepartmentId`, `movementTypeId`, `instructions`) which opens and sends the movement at once. The central number is a
server-issued GUID (`officialNumberText`); no number or year is accepted. `POST /api/documents/{id}/register` takes the same
optional register line and movement.

### Lookups — `/api/lookups/{lookup}`

`GET` (list, `?includeInactive=`), `GET /{lookup}/{id}` — `DOCUMENT_VIEW`;
`POST`, `PUT /{lookup}/{id}`, `PATCH /{lookup}/{id}/deactivate`, `PATCH /{lookup}/{id}/activate` —
`PATCH /{lookup}/{id}/set-default`, `PATCH /{lookup}/{id}/clear-default` —
`LOOKUP_MANAGE`. Unknown slug → 404. Entries carry `isDefault` (at most one per catalog; the SPA preselects it in
create-form selects; an inactive entry cannot be default → `409 Lookup.DefaultMustBeActive`; deactivating clears it).

33 lookup slugs (`GET /api/lookups`; `file-storage-providers` and `notification-types` reject `POST` with `400 Lookup.CatalogReadOnly`):

`access-levels`, `archive-event-types`, `archive-locations`, `assignment-types`,
`audit-action-types`, `audit-entity-types`, `department-types`,
`document-confidentiality-levels`, `document-directions`, `document-file-types`,
`document-number-event-types`, `document-number-scopes`, `document-number-statuses`,
`document-priority-levels`, `document-relation-types`, `document-status-types`, `document-types`,
`file-storage-providers`, `movement-event-types`, `movement-step-statuses`, `movement-statuses`,
`movement-types`, `notification-channels`, `notification-outbox-statuses`,
`notification-priorities`, `notification-types`, `numbering-modes`, `numbering-reset-policies`,
`organization-types`, `document-person-roles`, `genders`, `nationalities`, `participant-roles`, `participant-target-types`, `relation-action-types`,
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

## Notifications (Module 10) — `/api/notifications` and `/hubs/notifications` (implemented in the SPA: bell, `/notifications`, `features/notification-live`)

Every route only needs a valid token (no permission code) and is scoped to the caller. Another user's
row answers **404** `Notifications.RecipientNotFound`, exactly like a missing one.

| Method | Route | Notes |
| --- | --- | --- |
| `GET` | `/api/notifications` | Query `unreadOnly`, `typeId`, `search` (Arabic title/body), `sortBy` (`deliveredAt` default, `priority`, `type`, `isRead`), `sortDescending` (default `false`; with no `sortBy` the order is always newest first), `page`, `pageSize` (≤100). Paged `{ items, page, pageSize, totalCount, … }`, dismissed rows hidden. Item: `recipientId, notificationId, typeId, titleAr, bodyAr, priorityId, relatedDocumentId, relatedMovementId, actionUrl, isRead, readAt, createdAt`. |
| `GET` | `/api/notifications/unread-count` | `{ count, byType: [{ typeId, count }] }` (types with none omitted). Not recorded in the audit trail, so it is safe to poll. |
| `POST` | `/api/notifications/{recipientId}/read` | Idempotent. Use `recipientId`, not `notificationId`. |
| `POST` | `/api/notifications/read-all` | `{ count }` rows changed. |
| `POST` | `/api/notifications/{recipientId}/dismiss` | Idempotent; the row leaves the list and the count. |

`actionUrl` is a relative app path (`/documents/{id}`), validated on the server (starts with one `/`, no scheme or host).

Notification types: 1 document created, 2 registered, 3 movement sent, 4 received, 5 completed, 6 archived,
7 rejected, 8 returned, 9 redirected, 10 closed, 11 movement cancelled. Priorities 1 normal, 2 high, 3 urgent. IN_APP only.

**Who is told:** a sent or redirected movement goes to its assignee, else to the users of the destination
department; every other movement event goes to the source department and the movement creator; registration and
archive go to the document creator. The actor is never notified, and a SECRET or TOP_SECRET document is only
announced to its creator and grant holders.

**Hub** `/hubs/notifications` (SignalR, JSON): connect with the token as the `access_token` query string (the Vite
proxy already forwards `/hubs`). The server pushes `notificationReceived` with
`{ recipientId, notificationId, typeId, titleAr, bodyAr, priorityId, actionUrl, createdAt }` to the user's sessions
and closes the connection when the token expires. The push is only an alert: the database is the source of truth, so
reload the inbox and the count on every alert and on every reconnect. Delivery goes through an outbox (about 5 s
latency, retried with backoff, dead-lettered after 5 failures).

## Not yet implemented on the backend (front end must not call)

| Module | What exists | What is missing |
| --- | --- | --- |
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
   (Phase 2; the dialog also takes an optional recipient — external organization, internal department,
   or person — posted as a primary RECEIVER participant right after creation); `features/document-manage` + `pages/documents` details (participants, status history,
   timeline, draft-edit/register/participant/cancel) — ✅ done (Phase 3); `features/document-access-grant`
   + the `grants` tab — ✅ done (Phase 4); `entities/numbering-registry` + `features/numbering-manage`
   + `pages/numbering` + the document numbers tab — ✅ done (Phase 5); `entities/movement` +
   `features/movement-routing` + `widgets/movement-inbox` + `pages/movements` + the document movements
   tab — ✅ done (Phase 6); Module 06 attachments — ✅ done (`entities/document-file` +
   `features/attachment-upload` + `widgets/document-attachments-panel` + the `files` tab, Phases 0–5)
8. `features/external-organization-manage` + `widgets/external-organizations-table` + the
   `/documents?tab=organizations` tab (Module 03 Phase 7) — ✅ done
9. `entities/audit-log` + `features/audit-manage` + `widgets/audit-log-table` + `pages/audit`
   (search + integrity; respect self-audit side effect) — ✅ done (Module 09)
10. `widgets/visualization-panel` (cross-module Mermaid/chart switcher) + department/employee tree extras
11. Module 04 Numbering: pure template/scope model + status/event constants ✅ (Phase 1); `/numbering`
    tabs, URL-backed filters, duplicate-scope warnings, template preview/validation ✅ (Phase 2);
    department register book on the shared grid ✅ (Phase 3); document numbers grid + constants ✅
    (Phase 4); manager and read-only verification + registry inactive-department fix ✅ (Phase 5)

## Mandatory Documentation Rule (`AGENTS.md`)

Whenever you add, rename, delete, or modify files in any folder, you MUST update that folder's
`README.md` (`Contents`, `Implementation status`, and `Change Log` tables) in the same turn.
