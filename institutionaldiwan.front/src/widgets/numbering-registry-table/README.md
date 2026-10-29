# institutionaldiwan.front / src/widgets/numbering-registry-table — Numbering Registry Grid

> **Status:** ✅ Implemented (Module 03 Phase 5) and verified in-browser on `/numbering`.

## Purpose

The numbering registries administration grid: scope (department/type/direction), Arabic name,
format pattern, reset policy, per-year counters, active status, and permission-filtered row/header
menus for details, edit, activate/deactivate, and copy. Mounted by `pages/numbering`.

## Layer & Import Rules (FSD Strict Hierarchy)

- Allowed: `@/features/*`, `@/entities/*`, `@/shared/*` (public barrels).
- Forbidden: `@/app/*`, `@/pages/*`, sibling widgets.
- Export only the root widget via `index.ts`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/NumberingRegistryTable.tsx` | Client-side `DataTable` (`tableId="numbering-registries"`) over `useNumberingRegistries`; resolves department/type/direction labels from `entities/department` and `entities/lookup`; row menu (details always, edit/toggle behind `NUMBERING_MANAGE`, copy code/id), header/empty menus, create button, and the activate/deactivate `ConfirmDialog`. |
| `ui/RegistryDetailsDialog.tsx` | Read-only registry details: scope labels, format pattern, reset policy, status, and all yearly counters. |
| `index.ts` | Public barrel exporting `NumberingRegistryTable`. |

## Behaviour

- `NUMBERING_MANAGE` gates create/edit/toggle; without it the grid is read-only (details and copy
  remain) and the empty state explains that registries are managed by numbering administrators.
- Search matches code, Arabic name, format pattern, and resolved scope labels; sorting covers
  registry code, counter count, and creation date.
- Deactivation is destructive and clarifies that already-issued numbers are unchanged.

## Implementation status

| Item | State |
| --- | --- |
| Grid, menus, dialogs | ✅ Implemented (Module 03 Phase 5) |
| Browser verification | ✅ List/counters/scope/format/reset policy, create/edit/deactivate/activate, details dialog, read-only view for a `USER`-role caller |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Module 03 Phase 5: implemented the registry grid and details dialog with permission-filtered create/edit/activate actions and copy entries; verified in-browser. |
