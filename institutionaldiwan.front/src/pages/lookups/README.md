# `src/pages/lookups` — Reference Catalogs Page

> **Status:** Implemented; guarded by `LOOKUP_MANAGE`.

## Purpose

`/lookups`: composes `LookupCatalogPicker` and `LookupEntriesTable`. The selected catalog lives in
`?catalog=`; switching catalogs resets page, search, sort and filters.

## Contents

| File | Purpose |
| --- | --- |
| `ui/LookupsPage.tsx` | Permission guard, `GuideProvider pageId="lookups"`, header, picker and grid. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Page, route, sidebar entry «الجداول المرجعية» | ✅ |

## Change Log

- 2026-11-17: UI labels Phase 3 (`plans/ui-labels-edit-mode.md`): the page header title/description render from the catalog (`page.lookups.title` / `.description`) via `PageHeader` `titleKey`/`descriptionKey`.

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | Added the Module 01 lookups administration page. |
