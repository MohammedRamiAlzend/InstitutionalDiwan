# institutionaldiwan.front / src/shared/lib — Pure Utilities, Date/Timezone & Table State Helpers
> **Status:** Implemented for the Identity admin foundation — `cn`, theme store, column preferences, URL table state, debounce, Damascus date/number formatting, and JWT claim hints. Crypto and Arabic normalization land with Module 06 / search slices.
## Purpose
Contains pure, framework-independent helper functions for class merging, `Asia/Damascus` timezone formatting, Arabic text normalization, client-side SHA-256 file checksum calculation (matching Module 06 `DocumentFileVersion` requirements), per-table column-preference persistence, and the theme store.
## Contents
| File | Purpose |
| --- | --- |
| `cn.ts` | Combines `clsx` and `tailwind-merge` for conditional Tailwind class composition. |
| `theme.ts` | Theme store: reads/writes `diwan.theme` (`light` / `dark` / `system`), applies `data-theme`, follows `prefers-color-scheme` while in `system`, and exposes `useTheme` / `useThemeChoice` to React. |
| `date.ts` | Formats UTC ISO timestamps from the backend (`datetime2(3)`) into `Asia/Damascus` local dates/times with Latin digits. |
| `format.ts` | Latin-digit number grouping for counts, row ranges, and pagination labels; `formatShortId` renders the first GUID segment for compact LTR display (full value belongs in `title`). |
| `crypto.ts` | Computes lowercase 64-character hex SHA-256 checksums via `crypto.subtle.digest('SHA-256', buffer)` for attachment uploads. (❌ pending) |
| `arabic.ts` | Normalizes Arabic search strings (unified hamzas, taa marbuta, and Eastern/Western numerals). (❌ pending) |
| `columnPreferences.ts` | Reads/writes the per-table column visibility selection in `localStorage` under `diwan.table.<tableId>.columns.v1`; parses defensively, merges with the table's default columns, and exposes a reset to defaults. View state only — never sent to the API. |
| `useTableState.ts` | Table view state in URL search params (`page`, `pageSize`, `search`, `sort`, `dir`, plus caller-declared filter keys); resets to page 1 when search, filters, sort, or size change. |
| `useDebouncedValue.ts` | Generic debounce hook used by table search inputs. |
| `jwt.ts` | Decodes the JWT payload (no signature verification) and exposes the `must_change_password` UX hint the `/api/auth/me` response omits. |
| `index.ts` | Public barrel exporting the implemented helpers. |
## Implementation status
| Item | State |
| --- | --- |
| `cn.ts`, `theme.ts` | ✅ Implemented |
| `date.ts`, `format.ts`, `columnPreferences.ts`, `useTableState.ts`, `useDebouncedValue.ts`, `jwt.ts` | ✅ Implemented |
| `crypto.ts`, `arabic.ts` | ❌ Pending Module 06 / search slices |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | Added `formatShortId` for compact GUID display in the users grid fallback cells. |
| 2026-10-27 | agent | Implemented the table-state foundation (`useTableState`, `useDebouncedValue`, `columnPreferences`), Damascus `date`/`format` helpers, and `jwt` claim decoding for the forced password-change hint. |
| 2026-10-27 | agent | Implemented `cn.ts` and the `theme.ts` store (light/dark/system + `prefers-color-scheme`) used by the theme toggle and `ThemeProvider`. |
| 2026-10-25 | Buffy | Added `columnPreferences.ts` for the versioned per-table `localStorage` column chooser and documented its view-state-only rule. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/lib` segment and documented pure utility files. |
