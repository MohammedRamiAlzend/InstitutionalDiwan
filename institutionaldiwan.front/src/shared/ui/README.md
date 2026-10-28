# institutionaldiwan.front / src/shared/ui — RTL-First Design System & UI Kit Primitives
> **Status:** Implemented for the Identity admin foundation — core form primitives, dialogs, menus, select/checkbox, badges, skeletons, page headers, and the shared data grid. The Mermaid renderer lands with the visualization slice.
## Purpose
Holds reusable, domain-agnostic UI primitives (Tailwind CSS v4 + Radix UI where a primitive needs behavioral accessibility) built for Arabic-first (`dir="rtl"`) and bilingual (`ltr`) institutional interfaces. Components here know nothing about documents, departments, or users.
## Contents
| File | Purpose |
| --- | --- |
| `button.tsx` | Accessible button (CVA variants `primary`, `secondary`, `outline`, `ghost`, `destructive`; sizes `sm`/`md`/`lg`/`icon`) with a loading spinner state. |
| `input.tsx` | Form input with logical `ps-*`/`pe-*` spacing, start/end icons (mirrors in RTL automatically), and an invalid state. |
| `form-field.tsx` | Label + required marker + control + Arabic error message wrapper integrating with `react-hook-form`. |
| `password-field.tsx` | Password input built on `form-field` + `input` with its own show/hide toggle (`Eye`/`EyeOff`, Arabic `aria-label`); shared by the login, change-password, and admin reset forms. |
| `theme-toggle.tsx` | Light/dark toggle persisted in `diwan.theme`; used by the login page and future app shell. |
| `textarea.tsx` | Multi-line input matching `input.tsx` spacing, border, focus ring, and invalid state. |
| `select.tsx` | Radix Select with RTL-aware positioning, token-only styling, and an Arabic placeholder. |
| `checkbox.tsx` | Radix checkbox (checked / indeterminate) with an Arabic label and helper text. |
| `dialog.tsx` | Accessible centered modal with title, description, footer, size variants, overlay token, and entrance animation. Height is capped at the viewport (`max-h-[calc(100dvh-2rem)]`) with the title/footer pinned and a scrollable body, so long content never overflows the screen. |
| `confirm-dialog.tsx` | Confirmation gate for destructive commands built on `dialog` + `button`. |
| `dropdown-menu.tsx` | Radix dropdown wrapper sharing permission-filtered `MenuEntry` items; used for row overflow and the column chooser. |
| `context-menu.tsx` | The single right-click primitive: opens at the pointer, inherits direction, returns focus, and honors `Shift+F10` / `ContextMenu` dispatch from the grid. |
| `menu.ts` | Shared `MenuEntry` types (`MenuAction`, `MenuSeparator`, `MenuLabel`), style constants, `documentDirection()`, and the keyboard helpers `isContextMenuShortcut` / `dispatchContextMenu` used by tables and the app shell. |
| `badge.tsx` | Status/classification badge with `neutral`, `accent`, `success`, `warning`, `danger`, and `outline` variants. |
| `skeleton.tsx` | Pulse placeholder used for table and region loading states. |
| `page-header.tsx` | Page title, optional description/actions, and badge cluster. |
| `data-table.tsx` | The only real table: URL-driven filters/search/sort/pagination via `useTableState`, server and client modes, column chooser persisted per table in `localStorage` via `@/shared/lib/columnPreferences`, row/header/empty context menus, overflow row trigger, keyboard access, and Arabic loading/empty/error states. |
| `mermaid-view.tsx` | Lazy-loaded Mermaid renderer with a diagram / chart / tree view switcher and an Arabic-first invalid-syntax state. (❌ pending) |
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
| Core primitives (`button`, `input`, `form-field`, `password-field`, `theme-toggle`, `textarea`, `badge`, `skeleton`, `page-header`) | ✅ Implemented |
| Overlays (`dialog`, `confirm-dialog`, `select`, `checkbox`) | ✅ Implemented |
| Menus (`menu`, `context-menu`, `dropdown-menu`) | ✅ Implemented |
| Data grid (`data-table` with URL state and persisted columns) | ✅ Implemented (server + client modes) |
| `mermaid-view` | ❌ Pending the visualization slice |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Added `password-field` (form-field + input + visibility toggle) and moved `ChangePasswordForm` onto it, so the login, change-password, and admin reset forms share one password control. |
| 2026-10-28 | agent | Bounded the dialog to the viewport height and moved scrolling into a flex body with a pinned header/footer, so long panels (permission matrices, access + assignment views) scroll internally instead of spilling off screen; the role permission matrix now uses that single scroll area. |
| 2026-10-27 | agent | Exported the keyboard context-menu helpers (`isContextMenuShortcut`, `dispatchContextMenu`) through the barrel so the app shell can reuse the grid's `Shift+F10` / `ContextMenu` behavior. |
| 2026-10-27 | agent | Implemented the Identity admin primitives (`textarea`, `select`, `checkbox`, `dialog`, `confirm-dialog`, `badge`, `skeleton`, `page-header`, `menu`, `dropdown-menu`, `context-menu`) and the shared `data-table` with URL state, both paging modes, persisted column chooser, permission-filtered row/header/empty menus, keyboard `Shift+F10`, and Arabic empty/error states. |
| 2026-10-27 | agent | Implemented the core primitives required by the login slice, all consuming semantic tokens only (no hex) and logical properties (no physical `ml-*`/`left-*`). |
| 2026-10-25 | Buffy | Added the mandatory interaction primitives: `data-table` (URL filters/paging + localStorage column chooser), `context-menu` (permission-filtered right-click menus with keyboard support), and lazy `mermaid-view`; marked `table.tsx` as markup only. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/ui` segment with RTL logical-property rules and planned shadcn/Radix primitives. |
