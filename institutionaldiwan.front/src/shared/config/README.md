# institutionaldiwan.front / src/shared/config — Permissions, Routes & Environment Configuration

> **Status:** Implemented (permissions, routes, lookup ids, attachment upload limits, env).

## Purpose

Centralizes application constants, route paths, and the canonical permission codes shared with
`InstitutionalDiwan.Domain/Common/Permissions.cs` so route guards, sidebar items, and action buttons
never use magic strings.

## Contents

| File | Purpose |
| --- | --- |
| `permissions.ts` | Exports `Permissions` with all **20** codes (incl. `DOCUMENT_ARCHIVE`, `AUDIT_VIEW`, `AUDIT_MANAGE`), `PermissionCode`, and `AllPermissions`. Exact shape is specified in root `AGENTS.md`. |
| `features.ts` | `FEATURES`: parts of the product that are built but switched off for now. |
| `routes.ts` | Typed builders: `LOGIN`, `CHANGE_PASSWORD`, `DASHBOARD`, `DOCUMENTS`, `DOCUMENT_DETAIL(id)`, `DOCUMENT_DETAIL_PATTERN`, `MOVEMENTS`, `DEPARTMENTS`, `INSTITUTION_SETTINGS`, `USERS`, `ROLES`, `EMPLOYEES`, `NUMBERING`, `AUDIT` (no `/archive` until Module 07). |
| `lookupIds.ts` | Seeded IDs shared across entities: `USER_SCOPE_TYPE` (1–4), `ASSIGNMENT_TYPE` (1–2), `DEPARTMENT_TYPE` (1–5), `NUMBERING_MODE` (Automatic 1 / Manual 2), `NUMBERING_RESET_POLICY` (Yearly 1 / Monthly 2 / Never 3), `DOCUMENT_NUMBER_STATUS` (Active 1 / Cancelled 2 / Replaced 3), `DOCUMENT_NUMBER_EVENT_TYPE` (Issued 1 … ReRegisteredInDepartment 5), `DOCUMENT_DIRECTION` (Internal 1 / Incoming 2 / Outgoing 3 / All 4), `DOCUMENT_TYPE` (BaseRecord 1), `DOCUMENT_STATUS` (Draft 1 / Registered 2 / InMovement 3 / Archived 4 / Canceled 5), `DOCUMENT_CONFIDENTIALITY` (Normal 1 … TopSecret 4), `DOCUMENT_PRIORITY` (Normal 1 / Urgent 2 / Immediate 3), `PARTICIPANT_ROLE` (Sender 1 / Receiver 2 / Cc 3 / Signatory 4) with `PARTICIPANT_TARGET_TYPE` (ExternalOrganization 1 / InternalDepartment 2 / Person 3), `ORGANIZATION_TYPE` (Government 1 / Private 2 / International 3 / Individual 4), and the movement catalogs — `MOVEMENT_TYPE` (Referral 1 / Transfer 2 / Cc 3 / Return 4), `MOVEMENT_STATUS` (Draft 1 … Canceled 10), `MOVEMENT_STEP_STATUS` (Pending 1 … Redirected 7), `MOVEMENT_EVENT_TYPE` (Created 1 … Canceled 9), and the audit catalogs — `AUDIT_ACTION_TYPE` (Create 1 … Execute 8) and `AUDIT_ENTITY_TYPE` (Document 1 … Operation 13), plus the Module 06 `DOCUMENT_FILE_TYPE` (Scan 1 / Attachment 2 / Signature 3 / Other 4). Must match API seeds. |
| `env.ts` | Typed `import.meta.env` access (`API_BASE_URL` optional; default relative `/api`), `DEFAULT_TIMEZONE = 'Asia/Damascus'`. |
| `decisionLabels.ts` | Module 03 decision label resolvers: `decisionEffectLabel(effectId)`, `decisionStatusLabel(statusId)`, `appealOutcomeLabel(outcomeId)` map the seeded IDs to `decisions.*` catalog keys and resolve them through `t()` (with `{id}` fallbacks for unknown IDs), so they are renamable like any other label; `isDecisionInForce(statusId)` stays a pure ID check. Signatures are unchanged, so every consumer (register/types grids, `DecisionStatusBadge`, the document decisions panel, the issue/type dialogs) keeps working. |
| `attachmentLimits.ts` | Module 06 upload constraints the lookup response does not expose: `DOCUMENT_FILE_TYPE_LIMITS` (`maxBytes` + `allowedExtensions` per file type, mirroring `AttachmentsLookupSeeders.cs:37-45`), `MAX_UPLOAD_BYTES` (51 MB = `DocumentFilesController.MaxUploadBytes`), and `fileTypeLimits(fileTypeId)` which returns `null` for a type this client does not know so the server decides instead of a guessed limit blocking a valid upload. The server re-checks every rule and stays authoritative. |
| `guides/` | The tutorial-mode script: `types.ts` (`GuideEntry { id, title, body, note? }`, `PageGuide { pageId, intro, entryIds }`), `common.ts` (the six grid-tool entries `table.search` / `table.columns` / `table.pageSize` / `table.refresh` / `table.rowMenu` / `table.empty`, written once because `DataTable` renders them for every grid), one file per explained page (`users.ts` …), and `index.ts` exposing `getGuideEntry(id)` / `getPageGuide(pageId)`. Arabic UI copy only — user-facing text lives here, developer documentation stays in READMEs, and `npm run check:guides` fails if a JSX id has no entry or an entry has no user. |
| `labels/` | The editable UI-labels catalog (UI labels plan, `plans/ui-labels-edit-mode.md`): `common.ts` + `nav.ts` frozen Arabic defaults, aggregated by `index.ts` into `LABEL_DEFAULTS` with the derived `LabelKey` union (an unknown key is a build error). The app renders fully from these defaults; the server stores only administrator overrides. New area files join as the migration phases land. |
| `index.ts` | Public barrel exporting permissions, routes, environment, all lookup ID maps (including numbering, the correspondence catalogs, and `DOCUMENT_FILE_TYPE`), the attachment upload limits, the guide lookup functions, and the UI-labels catalog (`LABEL_DEFAULTS`, `LabelKey`). |

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
| `labels/` | ✅ Phase 1 starter (`common.ts` + `nav.ts`) + Phase 3 (`pages.ts`, `errors.ts`, expanded `common.ts`) + Phase 4 units (`documents.ts`, `movements.ts`, `decisions.ts` full area, `my-registers.ts`, `persons.ts`, `archive.ts` — ب1 complete; `departments.ts`, `institution-settings.ts`, `external-organizations.ts` — ب2 complete; `access.ts` — ب3 complete; `numbering.ts`, `document-schemas.ts`, `document-details.ts`, `attachments.ts`, `relations.ts`, `lookups.ts`, `audit.ts`, `system-errors.ts` — ب4 complete); `LABEL_DEFAULTS` + `LabelKey` through the barrel. Remaining surfaces (ب5) land in Phase 4. |
| `decisionLabels.ts` | ✅ Effect/status/appeal-outcome maps now hold `decisions.*` keys resolved via `t()` (UI labels Phase 4, decisions read surface); `isDecisionInForce` unchanged. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب5-متبقٍ): the `labels/` catalog gained `guides.ts` (`GUIDE_LABELS`) and `label-editor.ts` (`LABEL_EDITOR_LABELS`), and `common.ts` + `access.ts` gained the shared-chrome and entity-badge/picker/module keys, all aggregated into `LABEL_DEFAULTS`. |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب5): the `labels/` catalog gained `auth.ts` (`AUTH_LABELS`), `dashboard.ts` (`DASHBOARD_LABELS`), `notifications.ts` (`NOTIFICATIONS_LABELS`) and `print.ts` (`PRINT_LABELS`), and `audit.ts` gained the page-tab keys (`audit.tab.*` / `audit.page.*`), all aggregated into `LABEL_DEFAULTS`. |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب4 العلاقات): the `labels/` catalog gained `relations.ts` (`RELATIONS_LABELS`, `relation.*`) for the direction phrases/labels, the links grid, the panel view switch + hidden notice, the graph/tree, the relation history and the create/remove dialogs + picker + Zod, aggregated into `LABEL_DEFAULTS`. `entities/document-relation` direction maps now hold catalog keys (`relationPhraseKey`/`relationPhrase`/`RELATION_DIRECTION_LABELS`). |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب4 المرفقات): the `labels/` catalog gained `attachments.ts` (`ATTACHMENTS_LABELS`, `attachment.*`) for the document-files grid, the file-state/checksum badges, the local-checksum helper and the upload/version/archive/verify/versions dialogs + Zod, aggregated into `LABEL_DEFAULTS`. |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب4 تفاصيل الوثيقة): the `labels/` catalog gained `document-details.ts` (`DOCUMENT_DETAILS_LABELS`, `docdetails.*`, ~180 keys) for the document-details page tabs/sections, numbers/movements/timeline/grants surfaces, participants/persons tabs and the register/cancel/participant/grant dialogs + Zod, aggregated into `LABEL_DEFAULTS`; `documents.ts` gained the feature-flag-composed page description keys. |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب4 الرقابة والتدقيق): the `labels/` catalog gained `numbering.ts` (`NUMBERING_LABELS`), `document-schemas.ts` (`DOCUMENT_SCHEMAS_LABELS`), `lookups.ts` (`LOOKUPS_LABELS`), `audit.ts` (`AUDIT_LABELS`) and `system-errors.ts` (`SYSTEM_ERRORS_LABELS`), aggregated into `LABEL_DEFAULTS`; `documents.ts` gained the restricted-document and number-status keys. `lookupCatalogs.ts` now exposes `labelKey`/`groupKey` (catalog keys, null `labelKey` for server-added slugs) resolved by consumers via `resolveLabel`, and the audit module-name map holds `audit.module.*` keys. ب4 اكتمل. |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب3 إدارة الوصول): the `labels/` catalog gained `access.ts` (`ACCESS_LABELS`, `user.*` / `role.*` / `employee.*`), aggregated into `LABEL_DEFAULTS`. Covers the users/roles/employees grids, all their write dialogs and the three Zod schemas. ب3 اكتمل. |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب2 التنظيم): the `labels/` catalog gained `departments.ts` (`DEPARTMENTS_LABELS`), `institution-settings.ts` (`INSTITUTION_SETTINGS_LABELS`) and `external-organizations.ts` (`EXTERNAL_ORGANIZATIONS_LABELS`), aggregated into `LABEL_DEFAULTS`. Covers the hierarchy views + dialogs, register book, records panel, institution settings + logo, and external organizations. ب2 اكتمل. |
| 2026-11-18 | Qoder | UI labels Phase 4 (archive): the `labels/` catalog gained `archive.ts` (`ARCHIVE_LABELS`, `archive.*`) for the archive grid, saved-searches bar and archive-manage dialogs, aggregated into `LABEL_DEFAULTS`. ب1 (المراسلات) اكتمل. |
| 2026-11-18 | Qoder | UI labels Phase 4 (persons): the `labels/` catalog gained `persons.ts` (`PERSONS_LABELS`, `persons.*`) for the people list, person-details tabs, person-documents grid, timeline, add/change-role dialogs and `PersonNamesInput`, aggregated into `LABEL_DEFAULTS`. |
| 2026-11-18 | Qoder | UI labels Phase 4 (my-registers): the `labels/` catalog gained `my-registers.ts` (`MY_REGISTERS_LABELS`, `my-registers.*`) for the `/my-registers` browser, aggregated into `LABEL_DEFAULTS`. |
| 2026-11-18 | Qoder | UI labels Phase 4 (decisions read surface): `decisionLabels.ts` effect/status/appeal-outcome maps now hold `decisions.*` catalog keys resolved through `t()` (with `{id}` fallbacks); signatures unchanged so all consumers keep working. The `labels/` catalog gained `decisions.ts` (`DECISION_LABELS`). |
| 2026-11-18 | Qoder | UI labels Phase 4 (movements unit): the `labels/` catalog gained `movements.ts` (`MOVEMENT_LABELS`, `movements.*`) for the routing area — movements page tabs, inbox/outbox queue, action labels/dialogs, archive hint, assignee picker, timeline note templates, and Zod messages — aggregated into `LABEL_DEFAULTS`. |
| 2026-11-18 | Qoder | UI labels Phase 4 (documents list unit): the `labels/` catalog gained `documents.ts` (`DOCUMENT_LABELS`, `documents.*`) for the correspondence list screen — page tabs, registry table, `RegisterNumberFilter`, and the create dialog — aggregated into `LABEL_DEFAULTS`. |
| 2026-11-17 | Qoder | UI labels Phase 3 (`plans/ui-labels-edit-mode.md`): the `labels/` catalog grew to `common.ts` (actions, table tools, shell/account/settings chrome, `common.error.*`), `pages.ts` (static page titles/descriptions) and `errors.ts` (full ~358-code error catalog), all aggregated into `LABEL_DEFAULTS`. |
| 2026-11-17 | Qoder | UI labels, Phase 1 (`plans/ui-labels-edit-mode.md`): added `labels/` (`common.ts` + `nav.ts` frozen Arabic defaults → `LABEL_DEFAULTS` + derived `LabelKey`) and exported them through the barrel; the sidebar is the first consumer. |
| 2026-11-17 | Qoder | `documents.create` guide body now says the register is preselected from the choice made under «الوثائق» in the sidebar and can be changed in the form. |
| 2026-11-17 | Qoder | Details-tab layout redesign: `document-details.section-archive` became `document-details.section-logs` (the unified newest-first timeline of status/participant/number/archive events plus the department records); `tab-details` describes the sticky summary sidebar, the chip navigation and the collapsible sections; `section-numbers`/`section-grants` mention their collapsible headers. |
| 2026-11-16 | Qoder | Document-details guide follows the tab merge: the five tab entries (`tab-numbers`, `tab-grants`, `tab-fields`, `tab-department-records`, `tab-archive`) became `section-*` entries pointed at the merged regions inside the details tab; `document-details.tabs` and `tab-details` describe the six-tab order and the bundled «السجلات» section, and `tab-movements` describes the receive/redirect-only buttons. |
| 2026-11-11 | Claude | Added `ROUTES.MY_REGISTERS` (`/my-registers`) and the `my-registers` tutorial guide. |
| 2026-11-11 | Claude | `DOCUMENT_RELATION_TYPE.JoinedInto` is 5. The document-details guide explains the principal file and the join suggestion. The decisions guide no longer says unification cannot be issued. |
| 2026-11-11 | Claude | `APPEAL_OUTCOME` (1 pending, 2 accepted, 3 rejected) and the appeal guide entries on the document details page. A revoked decision is labelled «مفسوخ». |
| 2026-11-11 | Claude | Added `DECISION_MANAGE` (23 codes), `DECISION_EFFECT`, `DECISION_STATUS`, `DOCUMENT_FILE_TYPE.Decision`, `/decisions`, and the `decisions` tutorial guide. |
| 2026-11-11 | Claude | `MOVEMENT_TYPE.ToArchive` (5) and the `institution-settings.archive` guide entry. |
| 2026-11-10 | Claude | Added `DEPARTMENT_RECORD_VIEW_ALL` and `DEPARTMENT_RECORD_CORRECT` (22 codes) and the `document-details.tab-department-records` guide entry. |
| 2026-11-09 | Claude | Tutorial catalogue: added `table.export` to the page guides and `document-details.export-pdf`. |
| 2026-11-09 | Claude | Added `FEATURES.drafts` (off). |
| 2026-11-09 | Claude | `isLookupCatalogEnabled` hides the catalogs of suspended features. |
| 2026-11-09 | Claude | Added `FEATURES.participants` (off). |
| 2026-11-09 | Claude | Added `FEATURES.personParticipants` (off). |
| 2026-11-09 | Claude | Added `features.ts`: `FEATURES.externalOrganizations` is off, which suspends external organizations in the interface. |
| 2026-11-09 | Claude | Removed the `document-details.edit` guide entry. |
| 2026-11-09 | Claude | Added `PERSON_VIEW`/`PERSON_MANAGE` (20 codes), the persons routes and lookup catalog labels, and `guides/persons.ts`. |
| 2026-11-08 | Claude | `SYSTEM_ERRORS_VIEW` (18 codes), `ROUTES.SYSTEM_ERRORS` and the `system-errors` guide. |
| 2026-11-08 | Claude | Guide entry `archive.field-filters`; `documents.field-filters` describes the filters on the bar. |
| 2026-11-05 | Claude | `DOCUMENT_TYPE` is now `BaseRecord` (1) only; removed `DOCUMENT_TYPE_DIRECTION`. |
| 2026-11-04 | Claude | Registry filtering by dynamic field values (plan `plans/document-registry-field-filters.md`): added the `documents.field-filters` guide entry. |
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
