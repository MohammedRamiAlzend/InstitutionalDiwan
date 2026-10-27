# institutionaldiwan.front / src/shared/lib — Pure Utilities, Date/Timezone & Crypto Helpers
> **Status:** Partially implemented — `cn.ts` and `theme.ts` are live; the date, crypto, Arabic-normalization, and column-preference helpers land with their slices.
## Purpose
Contains pure, framework-independent helper functions for class merging, `Asia/Damascus` timezone formatting, Arabic text normalization, client-side SHA-256 file checksum calculation (matching Module 06 `DocumentFileVersion` requirements), per-table column-preference persistence, and the theme store.
## Contents
| File | Purpose |
| --- | --- |
| `cn.ts` | Combines `clsx` and `tailwind-merge` for conditional Tailwind class composition. |
| `theme.ts` | Theme store: reads/writes `diwan.theme` (`light` / `dark` / `system`), applies `data-theme`, follows `prefers-color-scheme` while in `system`, and exposes `useTheme` / `useThemeChoice` to React. |
| `date.ts` | Formats UTC ISO timestamps from the backend (`datetime2(3)`) into `Asia/Damascus` local dates/times and extracts the official numbering year. (❌ pending) |
| `crypto.ts` | Computes lowercase 64-character hex SHA-256 checksums via `crypto.subtle.digest('SHA-256', buffer)` for attachment uploads. (❌ pending) |
| `arabic.ts` | Normalizes Arabic search strings (unified hamzas, taa marbuta, and Eastern/Western numerals). (❌ pending) |
| `columnPreferences.ts` | Reads/writes the per-table column visibility selection in `localStorage` under `diwan.table.<tableId>.columns.v1`; parses defensively, merges with the table's default columns, and exposes a reset to defaults. View state only — never sent to the API. (❌ pending) |
| `index.ts` | Public barrel exporting the implemented helpers. |
## Implementation status
| Item | State |
| --- | --- |
| `cn.ts`, `theme.ts` | ✅ Implemented |
| `date.ts`, `crypto.ts`, `arabic.ts`, `columnPreferences.ts` | ❌ Pending their slices |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Implemented `cn.ts` and the `theme.ts` store (light/dark/system + `prefers-color-scheme`) used by the theme toggle and `ThemeProvider`. |
| 2026-10-25 | Buffy | Added `columnPreferences.ts` for the versioned per-table `localStorage` column chooser and documented its view-state-only rule. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/lib` segment and documented pure utility files. |
