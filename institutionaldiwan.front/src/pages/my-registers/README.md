# pages / my-registers

> **Status:** Implemented.

## Purpose

`/my-registers` («سجلاتي») lets a user choose one register of their own department and browse its
lines with search, filters and sort. It needs `NUMBERING_VIEW`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/MyRegistersPage.tsx` | The route: permission guard, guide provider, header and the `my-registers-table` widget. |
| `index.ts` | Public barrel. |

## Implementation status

| Item | State |
| --- | --- |
| Page | ✅ Implemented |

## Change Log

- 2026-11-17: UI labels Phase 3 (`plans/ui-labels-edit-mode.md`): the page header title/description render from the catalog (`page.my-registers.title` / `.description`) via `PageHeader` `titleKey`/`descriptionKey`.

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (my-registers): the `my-registers-table` widget now renders from the `my-registers.*` catalog (see the widget README). The page header already resolved from `page.my-registers.*` in Phase 3. |
| 2026-11-11 | Claude | The guide mentions filtering by form field values. |
| 2026-11-11 | Claude | The page text and guide mention the form data and its version. |
| 2026-11-11 | Claude | Added the «سجلاتي» page. |
