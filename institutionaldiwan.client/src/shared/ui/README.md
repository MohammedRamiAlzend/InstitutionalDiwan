# institutionaldiwan.client / src/shared/ui — RTL-First Design System & UI Kit Primitives
> **Status:** Scaffolding & Rules — UI kit rules defined; code pending.
## Purpose
Holds reusable, domain-agnostic UI primitives (`shadcn/ui` + Radix UI + Tailwind CSS v4) built for Arabic-first (`dir="rtl"`) and bilingual (`ltr`) institutional interfaces. Components here know nothing about documents, departments, or users.
## Contents
| File | Purpose |
| --- | --- |
| `button.tsx` | Accessible button with variants (`primary`, `secondary`, `outline`, `ghost`, `destructive`) and loading spinner state. |
| `input.tsx` / `textarea.tsx` | Form inputs supporting RTL text alignment, start/end icons (`ps-*` / `pe-*`), and validation error states. |
| `select.tsx` | Radix Select primitive with `dir="rtl"` support for lookup dropdowns. |
| `dialog.tsx` | Modal dialog primitive for create/edit/confirm workflows. |
| `table.tsx` | Base RTL table primitives (`Table`, `TableHeader`, `TableRow`, `TableHead`, `TableCell`); markup only — use `data-table.tsx` for real grids. |
| `data-table.tsx` | The only real table: URL-driven filters/search/sort/pagination, column chooser persisted per table in `localStorage` via `@/shared/lib/columnPreferences`, row/header/empty context menus, and an always-visible row overflow trigger. |
| `context-menu.tsx` | Radix `@radix-ui/react-context-menu` wrapper: callers pass permission-filtered items; opens at the pointer, supports `Shift+F10` / the `ContextMenu` key, and returns focus to the trigger on close. |
| `mermaid-view.tsx` | Lazy-loaded Mermaid renderer (`securityLevel: 'strict'`, capped input, HTML/script labels rejected, external links disabled) with a diagram / chart / tree view switcher and an Arabic-first invalid-syntax state. |
| `badge.tsx` | Status and classification badge primitive with semantic color variants. |
| `form-field.tsx` | Label + control + error message wrapper integrating cleanly with `react-hook-form`. |
| `index.ts` | Public barrel exporting all UI kit primitives. |
## Conventions / Notes
- Use logical CSS properties exclusively: Use `ms-*`, `me-*`, `ps-*`, `pe-*`, `start-*`, `end-*`, `text-start`, `text-end`, `rounded-s-*`, `rounded-e-*`—never `ml-*`, `mr-*`, `pl-*`, `pr-*`, `left-*`, `right-*`, or `text-left`/`text-right`.
- No domain imports: `shared/ui` may only import from `@/shared/lib` and external UI packages (`@radix-ui/*`, `lucide-react`, `class-variance-authority`).
- **No bespoke tables.** Features and widgets pass column definitions and a stable `tableId` to `data-table`; column visibility is view state stored under `diwan.table.<tableId>.columns.v1`, never URL or server state.
- **No bespoke right-click menus.** Callers supply menu item groups (already permission-filtered) to `context-menu` and keep the same actions reachable from the keyboard and an overflow trigger.
- `mermaid-view` is the only place that imports `mermaid`; import it lazily so it never enters the initial bundle.
## Implementation status
| Item | State |
| --- | --- |
| Core primitives (`button`, `input`, `select`, `dialog`, `table`, `badge`, `form-field`) | ❌ Not implemented |
| Interaction primitives (`data-table`, `context-menu`, `mermaid-view`) | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Added the mandatory interaction primitives: `data-table` (URL filters/paging + localStorage column chooser), `context-menu` (permission-filtered right-click menus with keyboard support), and lazy `mermaid-view`; marked `table.tsx` as markup only. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/ui` segment with RTL logical-property rules and planned shadcn/Radix primitives. |
