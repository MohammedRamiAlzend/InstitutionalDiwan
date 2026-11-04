# institutionaldiwan.front / src/shared/config — Permissions, Routes & Environment Configuration

> **Status:** Implemented (permissions, routes, lookup ids, attachment upload limits, env).

## Purpose

Centralizes application constants, route paths, and the canonical permission codes shared with
`InstitutionalDiwan.Domain/Common/Permissions.cs` so route guards, sidebar items, and action buttons
never use magic strings.

## Contents

| File | Purpose |
| --- | --- |
| `permissions.ts` | Exports `Permissions` with all **17** codes (incl. `DOCUMENT_ARCHIVE`, `AUDIT_VIEW`, `AUDIT_MANAGE`), `PermissionCode`, and `AllPermissions`. Exact shape is specified in root `AGENTS.md`. |
| `routes.ts` | Typed builders: `LOGIN`, `CHANGE_PASSWORD`, `DASHBOARD`, `DOCUMENTS`, `DOCUMENT_DETAIL(id)`, `DOCUMENT_DETAIL_PATTERN`, `MOVEMENTS`, `DEPARTMENTS`, `INSTITUTION_SETTINGS`, `USERS`, `ROLES`, `EMPLOYEES`, `NUMBERING`, `AUDIT` (no `/archive` until Module 07). |
| `lookupIds.ts` | Seeded IDs shared across entities: `USER_SCOPE_TYPE` (1–4), `ASSIGNMENT_TYPE` (1–2), `DEPARTMENT_TYPE` (1–5), `NUMBERING_MODE` (Automatic 1 / Manual 2), `NUMBERING_RESET_POLICY` (Yearly 1 / Monthly 2 / Never 3), `DOCUMENT_NUMBER_STATUS` (Active 1 / Cancelled 2 / Replaced 3), `DOCUMENT_NUMBER_EVENT_TYPE` (Issued 1 … ReRegisteredInDepartment 5), `DOCUMENT_DIRECTION` (Internal 1 / Incoming 2 / Outgoing 3 / All 4), `DOCUMENT_TYPE` (IncomingLetter 1 / OutgoingLetter 2 / InternalMemo 3 / Circular 4 / Decision 5), `DOCUMENT_STATUS` (Draft 1 / Registered 2 / InMovement 3 / Archived 4 / Canceled 5), `DOCUMENT_CONFIDENTIALITY` (Normal 1 … TopSecret 4), `DOCUMENT_PRIORITY` (Normal 1 / Urgent 2 / Immediate 3), `DOCUMENT_TYPE_DIRECTION` (type → direction pre-fill), `PARTICIPANT_ROLE` (Sender 1 / Receiver 2 / Cc 3 / Signatory 4) with `PARTICIPANT_TARGET_TYPE` (ExternalOrganization 1 / InternalDepartment 2 / Person 3), `ORGANIZATION_TYPE` (Government 1 / Private 2 / International 3 / Individual 4), and the movement catalogs — `MOVEMENT_TYPE` (Referral 1 / Transfer 2 / Cc 3 / Return 4), `MOVEMENT_STATUS` (Draft 1 … Canceled 10), `MOVEMENT_STEP_STATUS` (Pending 1 … Redirected 7), `MOVEMENT_EVENT_TYPE` (Created 1 … Canceled 9), and the audit catalogs — `AUDIT_ACTION_TYPE` (Create 1 … Execute 8) and `AUDIT_ENTITY_TYPE` (Document 1 … Operation 13), plus the Module 06 `DOCUMENT_FILE_TYPE` (Scan 1 / Attachment 2 / Signature 3 / Other 4). Must match API seeds. |
| `env.ts` | Typed `import.meta.env` access (`API_BASE_URL` optional; default relative `/api`), `DEFAULT_TIMEZONE = 'Asia/Damascus'`. |
| `attachmentLimits.ts` | Module 06 upload constraints the lookup response does not expose: `DOCUMENT_FILE_TYPE_LIMITS` (`maxBytes` + `allowedExtensions` per file type, mirroring `AttachmentsLookupSeeders.cs:37-45`), `MAX_UPLOAD_BYTES` (51 MB = `DocumentFilesController.MaxUploadBytes`), and `fileTypeLimits(fileTypeId)` which returns `null` for a type this client does not know so the server decides instead of a guessed limit blocking a valid upload. The server re-checks every rule and stays authoritative. |
| `guides/` | The tutorial-mode script: `types.ts` (`GuideEntry { id, title, body, note? }`, `PageGuide { pageId, intro, entryIds }`), `common.ts` (the six grid-tool entries `table.search` / `table.columns` / `table.pageSize` / `table.refresh` / `table.rowMenu` / `table.empty`, written once because `DataTable` renders them for every grid), one file per explained page (`users.ts` …), and `index.ts` exposing `getGuideEntry(id)` / `getPageGuide(pageId)`. Arabic UI copy only — user-facing text lives here, developer documentation stays in READMEs, and `npm run check:guides` fails if a JSX id has no entry or an entry has no user. |
| `index.ts` | Public barrel exporting permissions, routes, environment, all lookup ID maps (including numbering, the correspondence catalogs, and `DOCUMENT_FILE_TYPE`), the attachment upload limits, and the guide lookup functions. |

## Example `routes.ts` shape

```ts
export const ROUTES = {
  LOGIN: '/login',
  CHANGE_PASSWORD: '/change-password',
  DASHBOARD: '/',
  DOCUMENTS: '/documents',
  DOCUMENT_DETAIL: (id: string) => `/documents/${id}`,
  DOCUMENT_DETAIL_PATTERN: '/documents/:documentId',
  MOVEMENTS: '/movements',
  DEPARTMENTS: '/departments',
  INSTITUTION_SETTINGS: '/institution-settings',
  USERS: '/users',
  ROLES: '/roles',
  EMPLOYEES: '/employees',
  NUMBERING: '/numbering',
  AUDIT: '/audit',
} as const;
```

## Implementation status

| Item | State |
| --- | --- |
| `permissions.ts`, `routes.ts`, `lookupIds.ts`, `env.ts`, `index.ts` | Implemented, including `/institution-settings`, the `/audit` route, numbering ID maps, the Module 03 document/participant/organization/movement ID maps, and the Module 09 audit action/entity ID maps. |
| `DOCUMENT_NUMBER_STATUS`, `DOCUMENT_NUMBER_EVENT_TYPE` | ✅ Exported (Module 04 Phase 1) and consumed (Phase 4) by `NumberStatusBadge` and the document numbers history grid, so no numeric literals remain in those views. |
| `DOCUMENT_FILE_TYPE`, `attachmentLimits.ts` | ✅ Added in Module 06 Phase 0; consumed by the upload form's pre-validation and the files grid from Phase 3. |
| `guides/` | ✅ Tutorial catalogue complete for every page: `common.ts` (grid tools) plus one file per page (`users`, `roles`, `employees`, `movements`, `documents` incl. document details, `departments`, `numbering`, `institution-settings`, `audit`, `dashboard`) — 60 entries, checked by `npm run check:guides`. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | Added `ROUTES.LOOKUPS`, `lookupCatalogs.ts` (Arabic names, groups and `canCreate` for the 33 catalogs) and the `lookups` page guide. |
| 2026-11-04 | Claude | Module 10 notifications front end (plan `plans/module-10-notifications-frontend.md`): added `ROUTES.NOTIFICATIONS`, `ENV.HUB_BASE_URL`, `NOTIFICATION_TYPE` (1–11), `NOTIFICATION_PRIORITY` (1–3), `NOTIFICATION_SORT_KEY`, and the `notifications` page guide. |
| 2026-11-03 | Claude | Added `ROUTES.DOCUMENT_SCHEMAS` and the `document-schemas` page guide plus the `document-details.tab-fields` entry. |
| 2026-11-03 | agent | Module 08: added `DOCUMENT_RELATION_TYPE` and `RELATION_ACTION_TYPE` to `lookupIds.ts` and the relations entries (`document-details.tab-relations`, `document-details.relations-*`) to the document-details guide. |
| 2026-11-03 | agent | Module 08: added `DOCUMENT_RELATION_TYPE` and `RELATION_ACTION_TYPE` to `lookupIds.ts` and the relations entries (`document-details.tab-relations`, `document-details.relations-*`) to the document-details guide. |
| 2026-11-03 | agent | Module 07: added `ROUTES.ARCHIVE`, `ARCHIVE_EVENT_TYPE`/`ARCHIVE_LOCATION` in `lookupIds.ts`, and the `archive` page guide plus the archive entries of the document-details guide. |
| 2026-11-03 | Claude | Mirrored the API's new `DOCUMENT_ARCHIVE` permission (17 codes). No screen uses it yet; Module 07 endpoints do not exist. |
| 2026-11-02 | agent | Tutorial catalogue extended to all pages: added `roles`, `employees`, `movements`, `documents` (+ document details), `departments`, `numbering`, `institution-settings`, `audit` and `dashboard` guides and registered them in `guides/index.ts`. |
| 2026-11-01 | agent | Tutorial mode Phase 0: added `guides/` — the `GuideEntry`/`PageGuide` contracts, `common.ts` for the six grid-tool entries shared by every table, `users.ts` for the witness page, and `index.ts` with `getGuideEntry`/`getPageGuide` through the public barrel. Ids are namespaced (`<page>.<region>`, `table.<tool>`) so `scripts/check-guide-ids.mjs` can prove every id used exists and every entry written is used. |
| 2026-11-01 | agent | Module 06 Phase 0: added `DOCUMENT_FILE_TYPE` (Scan 1 / Attachment 2 / Signature 3 / Other 4) and `attachmentLimits.ts` — `DOCUMENT_FILE_TYPE_LIMITS` mirroring `AttachmentsLookupSeeders.cs:37-45`, `MAX_UPLOAD_BYTES` matching `DocumentFilesController.MaxUploadBytes`, and `fileTypeLimits()` returning `null` for an unknown type so the server decides instead of a guessed limit blocking a valid upload. The lookup read exposes ids and labels only, hence the client-side copy. |
| 2026-10-29 | agent | Module 04 Phase 4: confirmed `DOCUMENT_NUMBER_STATUS` and `DOCUMENT_NUMBER_EVENT_TYPE` are the only source of number status/event ids in the UI — `NumberStatusBadge` and the `DocumentNumbersTab` status/event label maps now reference them directly. |
| 2026-10-29 | agent | Module 04 Phase 1: added `DOCUMENT_NUMBER_STATUS` (1–3) and `DOCUMENT_NUMBER_EVENT_TYPE` (1–5) deterministic ID maps through the public barrel, replacing magic literals in the number badge and history tab. |
| 2026-10-29 | agent | Module 09: added the `AUDIT_ACTION_TYPE` and `AUDIT_ENTITY_TYPE` deterministic ID maps through the public barrel for the audit grid's lookups and filters. |
| 2026-10-29 | agent | Module 03 Phase 7: added the `ORGANIZATION_TYPE` deterministic ID map (Government 1 / Private 2 / International 3 / Individual 4) through the public barrel for the external-organization lookup, form, and grid. |
| 2026-10-29 | agent | Module 03 Phase 6: added the `MOVEMENT_TYPE`, `MOVEMENT_STATUS`, `MOVEMENT_STEP_STATUS`, and `MOVEMENT_EVENT_TYPE` deterministic ID maps (shared by the movement badges, transition rules, and routing dialogs) through the public barrel. |
| 2026-10-28 | agent | Module 03 Phase 3: added `PARTICIPANT_ROLE` and `PARTICIPANT_TARGET_TYPE` deterministic IDs for the participant dialog and details tab, and `DOCUMENT_DETAIL_PATTERN` for the registered `/documents/:documentId` route. |
| 2026-10-28 | agent | Module 03 Phase 2: added deterministic document direction/type/status/confidentiality/priority IDs and `DOCUMENT_TYPE_DIRECTION`, exported through the barrel for the registry and create dialog. |
| 2026-10-28 | agent | Document the institution-settings route and numbering mode/reset-policy IDs exposed through the public config barrel for Module 01. |
| 2026-10-27 | agent | Added `lookupIds.ts` (deterministic `UserScopeType`/`AssignmentType`/`DepartmentType` ids) so multiple entity slices share the seeds without sibling imports; verified against the live `/api/lookups` catalogs. |
| 2026-10-27 | agent | Implemented the segment exactly per the root `AGENTS.md` contracts (16 permissions, typed routes, relative API base URL). |
| 2026-10-25 | Cursor | Updated to 16 permissions (added audit); expanded routes for movements/audit; deferred archive route; linked AGENTS.md contract. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/shared/config` segment and documented canonical permission and route constants. |
