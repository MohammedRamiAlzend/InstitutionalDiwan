# pages / decisions

> **Status:** Implemented. Exercised in the browser: the register showed the canceled decision, and the types tab showed the five seeded types.

## Purpose

`/decisions` lists every decision the caller may view, and, for `LOOKUP_MANAGE`, the decision types. The tab lives in `?tab`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DecisionsPage.tsx` | The page, its guide and the two tabs (`decisions.tab.*` via `<Label>`, tablist aria via `t()`). Switching tabs clears the other search params. |
| `ui/DecisionRegisterTable.tsx` | Server-paged grid (`tableId` `decisions`) with type, status, department and year filters. Column headers use `headerKey`; filter placeholders use `useLabel`; menus/aria/caption/search/empty states use `t()`; the clear-filters button uses `<Label>`. Effect/status cells resolve through `decisionEffectLabel`/`DecisionStatusBadge`. |
| `ui/DecisionTypesTable.tsx` | Client grid (`tableId` `decision-types`) with activate, deactivate and the form dialog. Column headers use `headerKey`; appealable/active cells and the new-type button use `<Label>`; the status filter options/placeholder use `useLabel`; toasts/menus/aria/caption/search/empty states use `t()`. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Register and types tabs | ✅ |
| UI labels (read surface) | ✅ Phase 4: tabs, both grids' columns/filters/menus/cells/toasts/empty states render from `decisions.*`; no Arabic literals remain in `ui/`. Dialogs live in `features/decision-*` (later batch). |

## Change Log

- 2026-11-17: UI labels Phase 3 (`plans/ui-labels-edit-mode.md`): the page header title/description render from the catalog (`page.decisions.title` / `.description`) via `PageHeader` `titleKey`/`descriptionKey`.

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (decisions read surface): `DecisionsPage` tabs, `DecisionRegisterTable` and `DecisionTypesTable` now render from the `decisions.*` catalog — `headerKey` columns, `<Label>` cells/buttons, `useLabel` filter placeholders/options, `t()` menus/aria/caption/search/toasts/empty states. No Arabic literals remain in `ui/`. |
| 2026-11-11 | Claude | Added the decisions page. |
