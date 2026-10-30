# institutionaldiwan.front / src/widgets/numbering-registry-table — Numbering Registry Grid

> **Status:** ✅ Implemented (Module 03 Phase 5, extended in Module 04 Phase 2) and verified in-browser on `/numbering`.

## Purpose

The numbering registries administration grid: scope (department/type/direction), scope specificity,
Arabic name, format pattern, reset policy, per-year counters, active status, and permission-filtered
row/header menus for details, edit, activate/deactivate, and copy. Mounted by `pages/numbering`.

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/features/*`, `@/entities/*`, `@/shared/*` (public barrels).
- Forbidden: `@/app/*`, `@/pages/*`, sibling widgets.
- Export only the root widget via `index.ts`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/NumberingRegistryTable.tsx` | Client-side `DataTable` (`tableId="numbering-registries"`) over `useNumberingRegistries`; resolves department/type/direction labels from `entities/department` and `entities/lookup`; status/reset-policy/scope filters and a scope-specificity column, the duplicate active-scope banner (from `findDuplicateActiveScopes`); row menu (details always, edit/toggle behind `NUMBERING_MANAGE`, copy code/id), header/empty menus, create button, and the activate/deactivate `ConfirmDialog`. |
| `ui/RegistryDetailsDialog.tsx` | Read-only registry details: next-number preview for the current year, duplicate-scope warning, scope labels, format pattern, reset policy, status, neutral scope-precedence explanation, and all yearly counters. |
| `index.ts` | Public barrel exporting `NumberingRegistryTable`. |

## Behaviour

- `NUMBERING_MANAGE` gates create/edit/toggle; without it the grid is read-only (details and copy
  remain) and the empty state explains that registries are managed by numbering administrators.
- Search matches code, Arabic name, format pattern, and resolved scope labels; sorting covers
  registry code, scope specificity, counter count, and creation date.
- Status, reset-policy, and scope filters are URL-backed through `useTableState`; `مسح المرشحات`
  appears only when a filter is active.
- Deactivation is destructive and clarifies that already-issued numbers are unchanged.
- Two active registries with an identical scope raise a warning banner and an in-dialog warning;
  the copy never claims which registry the server will choose, because the backend precedence is
  not documented consistently.

## Implementation status

| Item | State |
| --- | --- |
| Grid, menus, dialogs | ✅ Implemented (Module 03 Phase 5) |
| Filters, specificity column, duplicate warning | ✅ Implemented (Module 04 Phase 2) |
| Browser verification | ✅ List/counters/scope/format/reset policy, create/edit/deactivate/activate, details dialog, read-only view for a `USER`-role caller; filters with URL persistence, specificity column, duplicate banner and dialog warning, next-number preview |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Module 04 Phase 2: added status/reset-policy/scope filters, the scope-specificity column, the duplicate active-scope banner, the details-dialog next-number preview and duplicate warning, and a neutral scope-precedence explanation; verified in-browser. |
| 2026-10-29 | agent | Module 03 Phase 5: implemented the registry grid and details dialog with permission-filtered create/edit/activate actions and copy entries; verified in-browser. |
