# institutionaldiwan.front / src/entities/external-organization — External Correspondents Read Slice

> **Status:** ✅ Implemented (2026-10-28) — paged list/search read, by-id read, query keys and hooks.
> Writes (create/update/activate/deactivate) stay out of Phase 3; the participant dialog only picks existing organizations.

## Purpose

Reads the Module 03 external correspondent catalog (`GET /api/external-organizations`) so the
document details tab can show the external party's name and the add-participant dialog can offer a
searchable picker. The list endpoint is server-paged (`PagedResult<ExternalOrganizationResponse>`
from `@/shared/types`), ordered by code, and hides deactivated rows unless `includeInactive` is set.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `ExternalOrganizationResponse` matching `InstitutionalDiwan.Application` and `ExternalOrganizationListParams` (paging/search plus `includeInactive`). |
| `api/externalOrganizationApi.ts` | Typed `GET /external-organizations` (paged) and `GET /external-organizations/{id}` through `httpClient`; blank search is dropped. |
| `model/queries.ts` | `externalOrganizationKeys` factory plus `useExternalOrganizations(params)` and `useExternalOrganizationById(id)`. |
| `index.ts` | Public barrel exporting types, API object, query keys, and hooks. |

## Permissions

Both reads require `DOCUMENT_VIEW`; the server enforces it, and the details page only mounts once
the caller can view the document. `organization-types` lookup labels the type id in the picker.

## Implementation status

| Item | State |
| --- | --- |
| `model/`, `api/`, `index.ts` | ✅ Implemented |
| Picker UI | Composed in `features/document-manage` (search + active-only list), not in this slice. |
| External organization writes | Out of Phase 3 scope (API live, no UI yet). |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Module 03 Phase 3: created the slice with the paged/searchable list read, by-id read, query keys, and hooks for participant names and the external-party picker. |
