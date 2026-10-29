# institutionaldiwan.front / src/entities/external-organization — External Correspondents Read Slice

> **Status:** ✅ Implemented (2026-10-28; admin-grid reads 2026-10-29) — paged list/search read,
> by-id read, query keys and hooks. Writes live in `features/external-organization-manage` (Module 03
> Phase 7); the participant dialog only picks existing active organizations.

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
| Admin grid | Composed in `widgets/external-organizations-table`, which also reads inactive rows via `includeInactive`. |
| External organization writes | ✅ Implemented in `features/external-organization-manage` (Module 03 Phase 7). |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Module 03 Phase 7: the same reads now back the `widgets/external-organizations-table` admin grid (including the `includeInactive` view); writes were implemented in `features/external-organization-manage`. |
| 2026-10-28 | agent | Module 03 Phase 3: created the slice with the paged/searchable list read, by-id read, query keys, and hooks for participant names and the external-party picker. |
