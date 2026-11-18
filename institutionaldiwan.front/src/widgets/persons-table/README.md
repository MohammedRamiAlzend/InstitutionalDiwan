# `src/widgets/persons-table`

> **Status:** Implemented and browser-verified. Plan: `plans/document-persons.md`.

## Purpose

The `/persons` grid (table id `persons`).

## Contents

| File | Purpose |
| --- | --- |
| `ui/PersonsTable.tsx` | Server-paged, searched and sorted list of people; a row opens the person's file. |
| `index.ts` | Public barrel. |

## Implementation status

| Area | Status |
| --- | --- |
| Slice | ✅ Implemented |
| UI labels | ✅ Phase 4 (persons): renders from `persons.*` — caption/search/row menu/empty states via `t()`, column headers via `headerKey`, the clear-search button via `<Label>`; refresh reuses `table.refresh`. Data-driven person names stay out. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (persons): the grid renders from the `persons.*` catalog. No Arabic literals remain. |
| 2026-11-09 | Claude | Created the slice. |
