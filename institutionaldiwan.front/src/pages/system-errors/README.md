# `src/pages/system-errors`

> **Status:** Implemented and verified against a live API on 2026-11-08.

## Purpose

`/system-errors`, behind `SYSTEM_ERRORS_VIEW`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/SystemErrorsPage.tsx` | Permission gate, guide provider, header and grid. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Everything listed above | ✅ Implemented |

## Change Log

- 2026-11-17: UI labels Phase 3 (`plans/ui-labels-edit-mode.md`): the page header title/description render from the catalog (`page.system-errors.title` / `.description`) via `PageHeader` `titleKey`/`descriptionKey`.

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-08 | Claude | Added for the developer's error log. |
