# institutionaldiwan.front / src/entities/lookup — Reference Lookups Catalog & Deterministic IDs Slice
> **Status:** ✅ Implemented (2026-10-27) — generic catalog reads, seeded fallbacks, deterministic IDs, and `LookupLabel`.
## Purpose
Reads the seeded reference ("lookup") catalogs through `GET /api/lookups/{lookup}` and resolves `(lookup, id)` pairs to Arabic labels. Named slugs cover `user-scope-types`, `assignment-types`, `department-types`, `access-levels`, `numbering-modes`, `numbering-reset-policies`, (2026-10-28) the five correspondence catalogs `document-directions`, `document-types`, `document-status-types`, `document-confidentiality-levels`, `document-priority-levels`, and the participant catalogs `participant-roles`, `participant-target-types`, (2026-10-29) the routing catalogs `movement-types`, `movement-statuses`, `movement-step-statuses`, `movement-event-types`, and the correspondent catalog `organization-types`, and (2026-10-29) the audit catalogs `audit-action-types`, `audit-entity-types`, and (2026-11-01) the attachment catalog `document-file-types`. Module 01 institution settings consumes the numbering catalogs, the Module 03 registry/create/participant flows consume the document and participant catalogs, the movement badges/routing dialogs consume the movement catalogs, the external-organization form/grid consume `organization-types`, the Module 09 audit grid consumes the audit catalogs, and the Module 06 upload form consumes `document-file-types` (whose per-type size/extension limits live in `shared/config/attachmentLimits.ts`, because the lookup response carries ids and labels only); the generic client also serves the API's other slugs.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `LookupEntryResponse` (API contract) and `LookupCatalogEntry` (`id`/`desc`/`descAr` fallback row). |
| `model/constants.ts` | Re-exports shared scope/assignment/department/numbering/document/movement/archive ID maps; defines `LOOKUP_SLUGS` and seeded Arabic `LOOKUP_FALLBACKS` for the named catalogs. |
| `api/lookupApi.ts` | `entries(lookup, includeInactive?)` → `GET /api/lookups/{lookup}`. |
| `api/lookupApi.ts` (catalogs) | `catalogs()` → `GET /api/lookups` (slugs of every supported catalog). |
| `model/queries.ts` | `lookupKeys` factory and `useLookupEntries(lookup, { includeInactive, enabled })` with `staleTime: Infinity` and seeded placeholder data (active-only reads). |
| `model/queries.ts` (catalogs) | `useLookupCatalogs()` and `lookupKeys.catalogs()` feed the `/lookups` picker. |
| `ui/LookupLabel.tsx` | Renders the Arabic label for an `(lookup, id)` pair from the live catalog, falling back to the seeded row, then `#id`; `null` renders `—`. |
| `index.ts` | Public barrel exporting types, IDs/slugs/fallbacks (including the document enums), query keys/hook, and `LookupLabel`. |
## Notes
- Deterministic IDs live in `src/shared/config/lookupIds.ts` so sibling entities (for example `entities/user`) can map `scopeTypeId` without importing another entity slice.
- Fallbacks keep labels usable before the first response and for callers without `LOOKUP_MANAGE`; the server remains the source of truth.
## Implementation status
| Item | State |
| --- | --- |
| Generic reads, labels, public barrel | Implemented. |
| Numbering slugs, ID re-exports, Arabic placeholders | Implemented for Module 01 settings; final browser verification pending. |
| Document catalogs (directions/types/statuses/confidentiality/priority) | ✅ Implemented for Module 03 Phase 2; browser verification pending. |
| Participant catalogs (roles/target types) | ✅ Implemented for Module 03 Phase 3. |
| Movement catalogs (types/statuses/step statuses/event types) | ✅ Implemented for Module 03 Phase 6; browser-verified through the movement badges and queues. |
| Organization types (`organization-types`) | ✅ Implemented for Module 03 Phase 7; browser-verified in the external-organization grid and form. |
| Audit catalogs (`audit-action-types`, `audit-entity-types`) | ✅ Implemented for Module 09; browser-verified as Arabic action/entity labels in the audit grid. |
| Document file types (`document-file-types`) | ✅ Slug and seeded Arabic placeholders added in Module 06 Phase 0; consumed since Phase 2 by the files grid (Arabic type column, type filter options, and type-aware search) and from Phase 3 by the upload form. Per-type size/extension limits are not in the response — see `shared/config/attachmentLimits.ts`. |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-11-11 | Claude | The relation-type fallback includes «موحَّد مع» (id 5). |
| 2026-11-11 | Claude | The document-file-type fallback includes «قرار» (id 5). |
| 2026-11-09 | Claude | Added the `DocumentPersonRoles` and `DocumentNumberEventTypes` slugs and the role fallbacks. |
| 2026-11-05 | Claude | Document-type fallbacks reduced to `سجل أساس`; removed `DOCUMENT_TYPE_DIRECTION`. |
| 2026-11-05 | Claude | Added `isDefault` to `LookupEntryResponse`, `findDefaultLookupEntry` and `usePreselectLookupDefault` (preselects the catalog default in empty create-form selects). |
| 2026-11-04 | Claude | Module 01 lookups administration: added `lookupApi.catalogs`, `lookupKeys.catalogs` and `useLookupCatalogs`. |
| 2026-11-03 | agent | Module 08: added `DocumentRelationTypes`/`RelationActionTypes` slugs, their ID re-exports and seeded Arabic fallbacks. |
| 2026-11-03 | agent | Module 07: added `ArchiveEventTypes`/`ArchiveLocations` slugs, `ARCHIVE_EVENT_TYPE`/`ARCHIVE_LOCATION` re-exports and their seeded Arabic fallbacks. |
| 2026-11-01 | agent | Module 06 Phase 2: the files grid became the first consumer of `document-file-types` — `LookupLabel` for the النوع column and `useLookupEntries` for the type filter options, so the seeded placeholders carry the labels before the catalog response arrives. |
| 2026-11-01 | agent | Module 06 Phase 0: added the `document-file-types` slug with the seeded Arabic placeholders (مسح ضوئي/مرفق/توقيع/أخرى) and the `DOCUMENT_FILE_TYPE` import used by the fallback map. |
| 2026-10-29 | agent | Module 09: added the `audit-action-types` and `audit-entity-types` slugs with seeded Arabic placeholders plus the `AUDIT_ACTION_TYPE` / `AUDIT_ENTITY_TYPE` ID re-exports used by the audit grid. |
| 2026-10-29 | agent | Module 03 Phase 7: added the `organization-types` slug with the seeded Arabic placeholders (حكومي/خاص/دولي/فرد) and the `ORGANIZATION_TYPE` re-export used by the external-organization form and grid. |
| 2026-10-29 | agent | Module 03 Phase 6: added the `movement-types`, `movement-statuses`, `movement-step-statuses`, and `movement-event-types` slugs with seeded Arabic placeholders, plus the shared movement ID re-exports used by the movement badges and routing dialogs. |
| 2026-10-28 | agent | Module 03 Phase 3: added `participant-roles` and `participant-target-types` slugs with deterministic IDs and seeded Arabic placeholders for the participant dialogs. |
| 2026-10-28 | agent | Module 03 Phase 2: added the five document lookup slugs, their deterministic IDs and seeded Arabic placeholders, `DOCUMENT_TYPE_DIRECTION`, and exported them through the barrel. |
| 2026-10-28 | agent | Document `numbering-modes` and `numbering-reset-policies`, their shared deterministic IDs, and seeded Arabic placeholders used by institution settings. |
| 2026-10-27 | agent | Implemented Phase 2 lookup reads for Module 02: generic catalog client, query hook with seeded placeholders, `LookupLabel`, and deterministic IDs moved to `shared/config`. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/lookup` slice folder and README. |
