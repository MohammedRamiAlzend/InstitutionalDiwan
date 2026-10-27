# institutionaldiwan.front / src/shared/ui — RTL-First Design System & UI Kit Primitives
> **Status:** Core primitives implemented (`button`, `input`, `form-field`, `theme-toggle`); the data grid, context menu, and Mermaid renderer land with their slices.
## Purpose
Holds reusable, domain-agnostic UI primitives (Tailwind CSS v4 + Radix UI where a primitive needs behavioral accessibility) built for Arabic-first (`dir="rtl"`) and bilingual (`ltr`) institutional interfaces. Components here know nothing about documents, departments, or users.
## Contents
| File | Purpose |
| --- | --- |
| `button.tsx` | Accessible button (CVA variants `primary`, `secondary`, `outline`, `ghost`, `destructive`; sizes `sm`/`md`/`lg`/`icon`) with a loading spinner state. |
| `input.tsx` | Form input with logical `ps-*`/`pe-*` spacing, start/end icons (mirrors in RTL automatically), and an invalid state. |
| `form-field.tsx` | Label + required marker + control + Arabic error message wrapper integrating with `react-hook-form`. |
| `theme-toggle.tsx` | Light/dark toggle persisted in `diwan.theme`; used by the login page and future app shell. |
| `select.tsx` | Radix Select primitive with `dir="rtl"` support for lookup dropdowns. (❌ pending) |
| `dialog.tsx` | Modal dialog primitive for create/edit/confirm workflows. (❌ pending) |
| `table.tsx` | Base RTL table primitives; markup only — use `data-table.tsx` for real grids. (❌ pending) |
| `data-table.tsx` | The only real table: URL-driven filters/search/sort/pagination, column chooser persisted per table in `localStorage` via `@/shared/lib/columnPreferences`, row/header/empty context menus, and an always-visible row overflow trigger. (❌ pending) |
| `context-menu.tsx` | Radix `@radix-ui/react-context-menu` wrapper: callers pass permission-filtered items; opens at the pointer, supports `Shift+F10`, and returns focus to the trigger on close. (❌ pending) |
| `mermaid-view.tsx` | Lazy-loaded Mermaid renderer with a diagram / chart / tree view switcher and an Arabic-first invalid-syntax state. (❌ pending) |
| `badge.tsx` | Status and classification badge primitive with semantic color variants. (❌ pending) |
| `index.ts` | Public barrel exporting the implemented primitives. |
## Conventions / Notes
- Use logical CSS properties exclusively: Use `ms-*`, `me-*`, `ps-*`, `pe-*`, `start-*`, `end-*`, `text-start`, `text-end`, `rounded-s-*`, `rounded-e-*`—never `ml-*`, `mr-*`, `pl-*`, `pr-*`, `left-*`, `right-*`, or `text-left`/`text-right`.
- No domain imports: `shared/ui` may only import from `@/shared/lib` and external UI packages (`@radix-ui/*`, `lucide-react`, `class-variance-authority`).
- **No bespoke tables.** Features and widgets pass column definitions and a stable `tableId` to `data-table`; column visibility is view state stored under `diwan.table.<tableId>.columns.v1`, never URL or server state.
- **No bespoke right-click menus.** Callers supply menu item groups (already permission-filtered) to `context-menu` and keep the same actions reachable from the keyboard and an overflow trigger.
- `mermaid-view` is the only place that imports `mermaid`; import it lazily so it never enters the initial bundle.
## Implementation status
| Item | State |
| --- | --- |
| Core primitives (`button`, `input`, `form-field`, `theme-toggle`) | ✅ Implemented |
| `select`, `dialog`, `table`, `badge` | ❌ Pending their slices |
| Interaction primitives (`data-table`, `context-menu`, `mermaid-view`) | ❌ Pending their slices |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Implemented the core primitives required by the login slice, all consuming semantic tokens only (no hex) and logical properties (no physical `ml-*`/`left-*`). |
| 2026-10-25 | Buffy | Added the mandatory interaction primitives: `data-table` (URL filters/paging + localStorage column chooser), `context-menu` (permission-filtered right-click menus with keyboard support), and lazy `mermaid-view`; marked `table.tsx` as markup only. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/ui` segment with RTL logical-property rules and planned shadcn/Radix primitives. |
