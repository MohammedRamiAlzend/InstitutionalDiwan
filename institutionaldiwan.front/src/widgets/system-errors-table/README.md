# `src/widgets/system-errors-table`

> **Status:** Implemented and verified against a live API on 2026-11-08.

## Purpose

The developer's grid of captured errors.

## Contents

| File | Purpose |
| --- | --- |
| `ui/SystemErrorsTable.tsx` | Full grid with filters in the URL, row menus, Excel export and the purge entry. |
| `ui/SystemErrorDetailsDialog.tsx` | Latest occurrence with its stack trace, status and notes. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Everything listed above | ✅ Implemented |
| UI labels migration (ب4) | ✅ Grid, filters, details dialog and purge resolve from the `system-error.*` catalog (2026-11-18) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب4): grid headers via `headerKey`, status/source/kind filter options via the `system-error.*` key maps + `resolveLabel`, cells/menus/caption/search/empty/aria/toasts via `t()`/`<Label>`. Fixed the columns `useMemo` dependency (`setSelectedId`) and typed the `resolveKeys` entries so the React Compiler and `tsc` gates pass. |
| 2026-11-08 | Claude | Added for the developer's error log. |
