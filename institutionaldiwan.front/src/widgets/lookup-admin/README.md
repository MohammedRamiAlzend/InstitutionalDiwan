# `src/widgets/lookup-admin` — Reference Catalog Administration

> **Status:** Implemented; build and lint pass, exercised against the live API.

## Purpose

The two halves of `/lookups`: a catalog picker grouped by module, and the entries grid of the
selected catalog with add, relabel and activate/deactivate actions.

## Contents

| File | Purpose |
| --- | --- |
| `ui/LookupCatalogPicker.tsx` | Lists `GET /api/lookups` slugs grouped by `shared/config/lookupCatalogs`, with a quick filter; unknown slugs fall under «أخرى». |
| `ui/LookupEntriesTable.tsx` | Client-mode `DataTable` (`tableId="lookup-entries"`, column preference `diwan.table.lookup-entries.columns.v1`): id, Arabic/English labels, status, created-at; status filter in the URL; row/header/empty menus; confirmation before deactivation with a warning for ids the front end hard-codes («نظامية»). |
| `index.ts` | Public barrel. |

## Conventions / Notes

- Always reads with `includeInactive=true`.
- «قيمة جديدة» is disabled for catalogs whose `canCreate` is false.

## Implementation status

| Item | State |
| --- | --- |
| Picker, grid, menus, confirmation | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-09 | Claude | The `organization-types` catalog is hidden while `FEATURES.externalOrganizations` is off. |
| 2026-11-05 | Claude | Entries grid shows an "افتراضية" badge and a context-menu action to set or clear the catalog default. |
| 2026-11-04 | Claude | Added the Module 01 lookups administration widgets. |
