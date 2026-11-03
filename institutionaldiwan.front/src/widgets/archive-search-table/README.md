# `src/widgets/archive-search-table` — Archive Search Grid

> **Status:** Implemented — URL-driven archive grid with saved searches.

## Purpose

Composes `entities/archive` reads and `features/archive-manage` saved-search commands into the
Module 07 grid on `/archive`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/ArchiveSearchTable.tsx` | The `archive-documents` data grid: server search/sort/paging, location/direction/type/department/date-range filters, row and header context menus, save-search toolbar, and the one-time application of the caller's default saved search. |
| `ui/SavedSearchesBar.tsx` | Chips for the caller's saved searches; apply rewrites the grid's URL params, with edit and delete actions through the `archive-manage` dialogs. |
| `index.ts` | Public barrel. |

## Notes

- A saved search stores exactly the grid's URL params (`search`, `sort`, `dir` and the filters), so
  restoring one never needs a server call; the server never runs the criteria.
- Rows hide nothing the server already filtered; a blanked title still renders as «وثيقة مقيّدة» without a link.

## Implementation status

| Item | State |
| --- | --- |
| `ui/ArchiveSearchTable.tsx`, `ui/SavedSearchesBar.tsx` | ✅ Implemented (build and lint pass; verified against the live API) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | agent | Implemented the archive grid and the saved-searches bar for Module 07. |
