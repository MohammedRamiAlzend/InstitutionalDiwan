# `src/widgets/person-documents-table`

> **Status:** Implemented and browser-verified. Plan: `plans/document-persons.md`.

## Purpose

A person's documents (table id `person-documents`).

## Contents

| File | Purpose |
| --- | --- |
| `ui/PersonDocumentsTable.tsx` | Role, status, current department or archive location, and last action, with role and status filters in the URL. |
| `index.ts` | Public barrel. |

## Implementation status

| Area | Status |
| --- | --- |
| Slice | ✅ Implemented |
| UI labels | ✅ Phase 4 (persons): renders from `persons.documents.*` — caption/search/aria/row menu/empty states via `t()`, column headers via `headerKey`, the «كل الصفات»/«كل الحالات» filter options via `useLabel`, the clear button via `<Label>`; the «تغيير الحالة» last-action cell reuses `persons.status-change`; refresh reuses `table.refresh`. Lookup `descAr` (roles/types/statuses/departments) stays out. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (persons): the grid renders from the `persons.documents.*` catalog. No Arabic literals remain. |
| 2026-11-09 | Claude | Created the slice. |
