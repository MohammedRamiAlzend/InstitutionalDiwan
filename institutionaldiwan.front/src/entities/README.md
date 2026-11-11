# institutionaldiwan.front / src/entities — Business Domain Entities Layer

> **Status:** Module 01 Organization reads (`department`, `institution-setting`), Module 02
> Identity reads (`user`, `role`, `employee`), lookup catalogs, the reusable employee picker,
> Module 03 document, external-organization, numbering-registry, and movement reads, the
> Module 06 `document-file` reads, and the Module 09 `audit-log` reads are implemented.

## Purpose

`src/entities/` models core institutional concepts on the client. Each slice owns TypeScript
read-model interfaces (matching backend `*Response` DTOs), TanStack Query hooks/key factories, and
read-only presentational UI. Entities never perform write mutations.

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/shared/*` only.
- Forbidden: upper layers and sibling entity slices (compose in widgets/features instead).
- Layout: `model/`, `api/`, `ui/`, `index.ts`.
- Deterministic lookup IDs live in `shared/config/lookupIds.ts` so several entities can consume them
  without importing `entities/lookup`.

## Contents — Entity Slices & Backend DTO Mapping

| Slice Folder | Backend DTOs & `GET` Endpoints | Key Read Components & Hooks | State |
| --- | --- | --- | --- |
| `user/` | `MeResponse`, `UserResponse`, effective permissions, `/identity-options/users` options | `useMeQuery` (session), `useUsers`, `useUserById`, `useUserEffectivePermissions`, `useUserOptions` (active-user picker), `UserScopeBadge` | ✅ Implemented |
| `department/` | `DepartmentResponse`, `DepartmentTreeNodeResponse` (`/departments`, `/tree`, `/{id}`) | `useDepartments`, `useDepartmentTree`, `useDepartmentById`, `DepartmentBadge`, `departmentTreeToMermaid` | Reads and department-only Mermaid builder implemented; the widget composes lazy employee branches. |
| `institution-setting/` | `InstitutionSettingResponse` (`/institution-settings`) | `useInstitutionSettings`, `institutionSettingKeys` | Singleton read implemented; missing settings remain an error. |
| `branding/` | `BrandingResponse` (`/branding`, anonymous) | `useBranding`, `brandingKeys`, `useBrandName`, `brandingLogoUrl`, `BrandMark`, `BrandName`, `BrandingEffects` | The institution name and logo shown by the sign-in page, shell, dashboard and browser tab. |
| `role/` | `RoleResponse`, `PermissionResponse` (`/roles`, `/permissions`) | `useRoles`, `useRoleById`, `usePermissionsCatalog`, `RoleBadge` | Implemented. |
| `employee/` | `EmployeeResponse` (paged `/employees`, `/{id}`) | `useEmployees`, `useEmployeeById`, `EmployeePicker` | Reads and reusable paged/searchable picker implemented. |
| `lookup/` | `LookupEntryResponse` (`/lookups/{lookup}`) | `useLookupEntries`, `LookupLabel`, seeded fallbacks | Named Organization/Identity catalogs (incl. numbering mode/reset policy) plus the five Module 03 document catalogs; generic reads implemented. |
| `document/` | `DocumentResponse`, `DocumentDetailResponse`, timeline, access grants, number history/registry entries (`/documents`, `/documents/{id}`) | `useDocuments`, `useDocumentById`, `useDocumentDetails`, `useDocumentTimeline`, `useDocumentAccessGrants`, `useDocumentNumberHistory`, `useDocumentRegistryNumbers`, `DocumentStatusBadge`, `DocumentDirectionBadge`, `OfficialNumberTag`, `NumberStatusBadge` | ✅ Reads and presentational UI implemented; the grants/numbers UI lives in the page and writes in `features/document-access-grant` / `features/numbering-manage`. |
| `external-organization/` | `ExternalOrganizationResponse` (paged `/external-organizations`, `/{id}`) | `useExternalOrganizations`, `useExternalOrganizationById` | ✅ Reads for the participant tab, picker, and admin grid; writes live in `features/external-organization-manage` (Phase 7). |
| `person/` | People named on documents: suggestions, list, file, documents, timeline (`personKeys`), and the `PersonNamesInput` field. | | |
| `decision-type/` | Decision types (`GET /api/decision-types`) | `useDecisionTypes`, `decisionTypeKeys` | Reads only. |
| `document-decision/` | A document's decisions and their appeals, the register, the suggested next action, and join suggestions | `useDocumentDecisions`, `useDocumentNextAction`, `useJoinCandidates`, `useDecisionRegister`, `documentDecisionKeys`, `DecisionStatusBadge` | Reads only. |
| `document-file/` | Module 06 file + version + checksum responses (`GET /api/documents/{id}/files`, `/document-files/{id}/download`, `/document-files/{id}/verify`) | `useDocumentFiles`, `useVerifyChecksum`, `documentFileKeys`, `documentFileApi.download`, `ChecksumBadge`, `FileArchivedBadge` | ✅ Reads implemented (Module 06 Phases 0 and 2) and mounted by `widgets/document-attachments-panel`; writes land in `features/attachment-upload` (Phase 3). |
| `numbering-registry/` | Registry/counter responses, department register book (`/numbering/registries`, `/departments/{id}/register`) | `useNumberingRegistries`, `useNumberingRegistryById`, `useDepartmentRegister`, `RegistryScopeCard`, pure `renderNumberPattern`/scope helpers | ✅ Reads implemented (Phase 5); pure template/scope model added in Module 04 Phase 1. |
| `movement/` | `MovementQueueItemResponse`, `MovementResponse` (steps/events) (`GET /api/movements/inbox`, `/outbox`, `/by-document/{documentId}`, `/{movementId}`) | `useMovementQueue`, `useMovementInbox`, `useMovementOutbox`, `useDocumentMovements`, `useMovementById`, `getMovementActions`/`hasActiveMovement` rules, `MovementStatusBadge`, `MovementStepStatusBadge`, `MovementTypeBadge` | ✅ Reads, transition rules, and badges implemented (Phase 6). |
| `system-error/` | `SystemErrorSummaryResponse`, `SystemErrorDetailsResponse` (`/system-errors`): list, details, Excel export, keys and labels. | — | — |
| `audit-log/` | `AuditLogResponse`, `AuditIntegrityCheckResponse` (paged `/audit-logs`, `/audit-logs/integrity-checks`) | `useAuditLogs`, `useAuditIntegrityReports`, `AUDIT_MODULES`/`auditModuleLabel`, `HashChainPill`, `IntegrityStatusBadge` | ✅ Reads, Arabic module labels, and badges implemented; writes live in `features/audit-manage`. |
| `archive/` | `ArchivedDocumentResponse`, `DocumentArchiveEventResponse`, `SavedSearchResponse` (`GET /api/archive/documents`, `/api/documents/{id}/archive-events`, `/api/saved-searches`) | `useArchivedDocuments`, `useDocumentArchiveEvents`, `useSavedSearches`, `archiveKeys`, `ArchiveEventTypeBadge` |
| `notification/` | `NotificationResponse`, `UnreadCountResponse` (`GET /api/notifications`, `/api/notifications/unread-count`) | `useNotifications`, `useUnreadCount`, `notificationKeys`, `NotificationItem`, `NotificationPriorityBadge`, `NotificationTypeIcon` |
| `document-relation/` | `DocumentRelationResponse`, `RelationGraphResponse`, `DocumentRelationEventResponse` (`GET /api/documents/{id}/relations`, `/graph`, `/events`) | `useDocumentRelations`, `useRelationGraph`, `useRelationEvents`, `relationGraphToMermaid`, `RelationTypeBadge` |
| `document-schema/` | `DocumentFieldResponse`, `SchemaVersionResponse`, `DocumentFieldsResponse` (`/document-types/{id}/fields`, `/schema`, `/schema-versions`, `/schema-versions/{id}`, `/documents/{id}/fields`) | `useDocumentTypeFields`, `useSchemaVersions`, `useSchemaVersion`, `useDocumentTypeSchema`, `useDocumentFields`, `DynamicFieldInputs`, value validation helpers, badges |

## Conventions

- Deterministic lookup ID constants live in `shared/config/lookupIds.ts`, re-exported by
  `entities/lookup/model/constants.ts`; never import a sibling entity for them.
- Every slice exports a typed query key factory for feature invalidation.
- Audit list reads are side-effecting on the server (self-audit row) — treat refetch as costly.
- Pure domain diagram builders live in the owning entity's `model/`, currently
  `department/model/toMermaid.ts` (departments only). `department/model/toTree.ts` does not exist:
  the hierarchy widget renders API tree nodes and composes lazy employee rosters across entities.
  Movement diagram builders remain planned. Rendering belongs to shared UI/widgets, not data models.
- Pure formatting/scope helpers follow the same rule: `numbering-registry/model/format.ts` (official
  number rendering and unknown-token detection) and `numbering-registry/model/scope.ts` (scope
  rank/key, duplicates, Arabic description) hold no React or API code.

## Implementation status

| Entity Slice | State |
| --- | --- |
| `user/`, `role/`, `employee/`, `lookup/` | ✅ Implemented |
| `department/` | Reads and department-only Mermaid builder implemented; lazy employee composition lives in the widget. |
| `institution-setting/` | Singleton read, query keys, and response type implemented. |
| `branding/` | Anonymous branding read, name hook, logo mark and tab title/icon effect implemented. |
| `employee/` picker and numbering lookups | Implemented for Module 01 forms; final verification pending. |
| `document/` | ✅ Read models, query keys/hooks, status/direction badges, the official-number tag, number history/registry reads, and `NumberStatusBadge`. |
| `external-organization/` | ✅ Paged/searchable list and by-id reads for participant names, the external-party picker, and the Phase 7 admin grid (`includeInactive`, `active`-only default). |
| `numbering-registry/` | ✅ Registry/counter and department-register reads, `RegistryScopeCard`, and pure `model/format.ts`/`model/scope.ts` helpers (template rendering/validation, scope rank/key, duplicate-scope detection, Arabic scope summary). |
| `movement/` | ✅ Queue/detail reads, mirrored transition rules (`getMovementActions`, `hasActiveMovement`, `translateMovementNote`), and three badges. |
| `audit-log/` | ✅ Paged log and integrity-report reads, query keys/hooks (five-minute `staleTime`, no focus refetch), module labels, hash pill, integrity badge. |
| `archive/` | ✅ Archive search, archive history and saved-search reads, query keys/hooks, saved-search criteria helpers, event-type badge (Module 07). |
| `notification/` | ✅ Inbox and unread-count reads (search, sort, filters, per-type counts), user-scoped query keys, row/priority/type-icon UI (Module 10). |
| `document-relation/` | ✅ Links, graph and history reads, query keys/hooks, direction phrases, Mermaid/tree builders, badges (Module 08). |
| `document-file/` | ✅ File/version/checksum types, `documentFileApi` (document-scoped list, blob download, on-demand verify), query keys/hooks (verify disabled until an explicit action), `ChecksumBadge`, and `FileArchivedBadge`; mounted by `widgets/document-attachments-panel`. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-12 | Claude | Added `branding/`: the institution name and logo read from the anonymous `GET /api/branding`, with `BrandMark`, `BrandName` and `BrandingEffects`. |
| 2026-11-11 | Claude | A document decision's appeals are part of the read model. |
| 2026-11-11 | Claude | `document-decision` reads join candidates. `document-relation` phrases type «موحَّد مع». |
| 2026-11-11 | Claude | Added `decision-type` and `document-decision` read models, query keys and the decision status badge. |
| 2026-11-09 | Claude | `DocumentResponse` no longer has `documentDate`. |
| 2026-11-09 | Claude | Added `person/`. |
| 2026-11-08 | Claude | Added `system-error/`. |
| 2026-11-04 | Claude | Module 10 notifications front end (plan `plans/module-10-notifications-frontend.md`): implemented the `notification` read slice. |
| 2026-11-03 | Claude | Added `document-schema/` for the dynamic document fields: reads, types, value helpers and the shared `DynamicFieldInputs` control set. |
| 2026-11-03 | agent | Module 08: implemented the `document-relation` read slice. |
| 2026-11-03 | agent | Module 07: implemented the `archive` read slice — archive search, per-document archive history and saved-search reads, `archiveKeys`, criteria (de)serialisation for saved searches, and the event-type badge. |
| 2026-11-01 | agent | Module 06 Phase 2: added `documentFileApi.download(fileId, versionId?)`, the blob read behind the files grid's «تنزيل الإصدار الحالي»; the slice now has a consumer, `widgets/document-attachments-panel` on the details page's «الملفات» tab. |
| 2026-11-01 | agent | Module 06 Phase 0: implemented the `document-file` read slice — file/version/checksum types mirroring `DocumentFileResponse`/`DocumentFileVersionResponse`/`AttachmentChecksumResponse`, `documentFileApi` on the document-scoped list route plus the on-demand verify read, `documentFileKeys` with `byDocument`/`verify`, `useDocumentFiles` (30-second `staleTime`, previous-page placeholder), `useVerifyChecksum` (disabled until the caller opts in, because it reads the stored bytes), and `ChecksumBadge`/`FileArchivedBadge`. No page consumes it yet — the `files` tab stays disabled until Phase 2. |
| 2026-10-29 | agent | Module 04 Phase 2: added `unknownPatternTokens` to `numbering-registry/model/format.ts` for form-time template validation. |
| 2026-10-29 | agent | Module 04 Phase 1: added pure `numbering-registry/model/format.ts` and `model/scope.ts` (template rendering, scope rank/key, duplicate active scopes, Arabic scope summary) through the slice barrel. |
| 2026-10-29 | agent | Module 09: implemented the `audit-log` read slice — paged log and integrity-report clients, `auditLogKeys`, `useAuditLogs`/`useAuditIntegrityReports` with a five-minute `staleTime` and no focus refetch (each read appends a self-audit row), the Arabic `AUDIT_MODULES` labels, `HashChainPill`, and `IntegrityStatusBadge`. |
| 2026-10-29 | agent | Module 03 Phase 7: reused the `external-organization` reads for the admin grid (`includeInactive` toggle, active-only picker) and moved the write-side note from "out of Phase 3" to `features/external-organization-manage`. |
| 2026-10-29 | agent | Module 03 Phase 6: implemented the `movement` read slice — inbox/outbox/by-document/detail API client, `movementKeys` (15-second queue `staleTime`), the mirrored transition rules, `translateMovementNote` for the API's English system notes, and movement/step/type badges. Mermaid/tree builders stay deferred to Module 05. |
| 2026-10-29 | agent | Module 03 Phase 5: implemented the `numbering-registry` read slice (registries/counters, department register book, query keys/hooks, `RegistryScopeCard`) and extended `document` with number history/registry-entry reads plus `NumberStatusBadge`. |
| 2026-10-28 | Qoder | Module 03 Phase 4: extended `user` with the `identity-options/users` option read (`UserOption`, `useUserOptions`, paged/searchable, previous-page placeholder) so the access-grant picker does not need `USER_MANAGE`. |
| 2026-10-28 | Qoder | Module 03 Phase 3: added the `external-organization` read slice (paged/searchable list, by-id, query keys/hooks) for participant names and the external-party picker. |
| 2026-10-28 | Qoder | Module 03 Phase 2: extended `lookup` with the document direction/type/status/confidentiality/priority catalogs, their seeded Arabic rows and `DOCUMENT_TYPE_DIRECTION`. |
| 2026-10-28 | Qoder | Module 03 Phase 1: implemented the `document` entity reads (list/by-id/details/timeline/grants), query keys, status/direction badges, and the official-number tag. |
| 2026-10-28 | agent | Document institution-setting reads, the reusable employee picker, numbering catalogs, and widget-owned lazy employee composition for Module 01. |
| 2026-10-28 | agent | Added `department/model/toMermaid.ts` (pure `DepartmentTreeNodeResponse[]` → Mermaid `flowchart TD`, label sanitizing, inactive marker) for the Module 01 hierarchy widget; `toTree` still waits on employee data. |
| 2026-10-27 | agent | Completed Phase 2: implemented `department`, `role`, `employee`, and `lookup` reads and extended `user` with admin list/detail/effective-permission hooks; all shapes verified against the live API. Moved deterministic lookup ids to `shared/config`. |
| 2026-10-27 | agent | Marked the `user/` session parts (`useMeQuery`, session store, `UserScopeBadge`) as implemented; user/employee list hooks remain pending. |
| 2026-10-25 | Buffy | Documented pure `model/` builders (`toTree`, `toMermaid`) so entity read models feed the shared tree/Mermaid visualization without owning UI. |
| 2026-10-25 | Cursor | Added `document-file`, `movement`, and `audit-log` entity slices mapped to live Modules 05/06/09 APIs. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/entities` FSD layer folder and documented domain entity slices, query key conventions, and lookup handling. |
