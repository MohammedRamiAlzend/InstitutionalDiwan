# institutionaldiwan.front / src/shared/lib — Pure Utilities, Date/Timezone & Table State Helpers
> **Status:** Implemented for the Identity admin foundation — `cn`, theme store, tutorial-mode store, column preferences, URL table state, debounce, Damascus date/number formatting, and JWT claim hints — plus the Module 06 attachment helpers: `formatFileSize`, client-side SHA-256, and blob save/`Content-Disposition` parsing. Arabic search normalization still pending.
## Purpose
Contains pure, framework-independent helper functions for class merging, `Asia/Damascus` timezone formatting, Arabic text normalization, client-side SHA-256 file checksum calculation (matching Module 06 `DocumentFileVersion` requirements), file download plumbing, per-table column-preference persistence, and the theme store.
## Contents
| File | Purpose |
| --- | --- |
| `cn.ts` | Combines `clsx` and `tailwind-merge` for conditional Tailwind class composition. |
| `theme.ts` | Theme store: reads/writes `diwan.theme` (`light` / `dark` / `system`), applies `data-theme`, follows `prefers-color-scheme` while in `system`, and exposes `useTheme` / `useThemeChoice` to React. |
| `tutorial.ts` | Tutorial-mode store: reads/writes `diwan.tutorial` (`on` / `off`), applies `html[data-tutorial]`, and exposes `useTutorial`. Only `on` is stored — an absent key is the off default, and unlike `motion` there is no system signal to follow. It lives here rather than in `widgets/app-shell` because `shared/ui/data-table` and `form-field` read it too, and a lower layer cannot import upward. |
| `date.ts` | Formats UTC ISO timestamps from the backend (`datetime2(3)`) into `Asia/Damascus` local dates/times with Latin digits. |
| `format.ts` | Latin-digit number grouping for counts, row ranges, and pagination labels; `formatShortId` renders the first GUID segment for compact LTR display (full value belongs in `title`); `formatFileSize` renders byte counts as Latin-digit sizes with Arabic units (بايت/ك.ب/م.ب/غ.ب) for attachment and version rows. |
| `crypto.ts` | `sha256Hex(blobOrBuffer)` computes the lowercase 64-character hex SHA-256 the server records per version, via `crypto.subtle.digest`. Throws `ChecksumUnsupportedError` (Arabic message) when `crypto.subtle` is absent, which happens only on an insecure origin — neither `https` nor `localhost`. |
| `download.ts` | `parseContentDisposition(header)` extracts a display file name, preferring RFC 5987 `filename*=UTF-8''…` over a plain `filename=` and stripping any path segments; `saveBlobAsFile(blob, fileName)` triggers the browser download through a temporary object URL. |
| `arabic.ts` | Normalizes Arabic search strings (unified hamzas, taa marbuta, and Eastern/Western numerals). (❌ pending) |
| `columnPreferences.ts` | Reads/writes the per-table column visibility selection in `localStorage` under `diwan.table.<tableId>.columns.v1`; parses defensively, merges with the table's default columns, and exposes a reset to defaults. View state only — never sent to the API. |
| `useTableState.ts` | Table view state in URL search params (`page`, `pageSize`, `search`, `sort`, `dir`, plus caller-declared filter keys); resets to page 1 when search, filters, sort, or size change. Page size defaults to 10 and is clamped to `MIN_PAGE_SIZE`..`MAX_PAGE_SIZE` (1..100, the same ceiling as the server's `PageRequest.MaxPageSize`); both bounds are exported so the grid can offer them as input limits, and a value equal to the effective `defaultPageSize` stays out of the URL. `clearQuery()` removes the search and every filter in one URL write, because two sequential setters would each rebuild from the same stale params and restore what the other cleared. |
| `useDebouncedValue.ts` | Generic debounce hook used by table search inputs. |
| `jwt.ts` | Decodes the JWT payload (no signature verification) and exposes the `must_change_password` UX hint the `/api/auth/me` response omits. |
| `index.ts` | Public barrel exporting the implemented helpers. |
## Implementation status
| Item | State |
| --- | --- |
| `cn.ts`, `theme.ts` | ✅ Implemented |
| `date.ts`, `format.ts`, `columnPreferences.ts`, `useTableState.ts`, `useDebouncedValue.ts`, `jwt.ts`, `tutorial.ts` | ✅ Implemented |
| `crypto.ts`, `download.ts` | ✅ Implemented (Module 06 Phase 1) |
| `arabic.ts` | ❌ Pending the search slice |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-11-01 | agent | Tutorial mode Phase 0: added `tutorial.ts` — `getTutorialChoice` / `applyTutorial` / `setTutorialChoice` / `useTutorial` over `diwan.tutorial` and `html[data-tutorial]`, with only `on` stored so the off default leaves no key. The store is in `shared/lib` rather than beside `density.ts`/`motion.ts` because `shared/ui/data-table` and `form-field` must read it, and `shared` may not import from `widgets`. |
| 2026-11-01 | agent | Lowered `useTableState`'s page-size default from 20 to 10 and exported `MIN_PAGE_SIZE` / `MAX_PAGE_SIZE` (1..100, matching the server's `PageRequest.MaxPageSize`) so the grid can offer them as input limits; `setPageSize` now treats the caller's `defaultPageSize` as the value that stays out of the URL, not the module constant, so a grid-specific default is not written to the query string. |
| 2026-11-01 | agent | Module 06 Phase 1: added `crypto.ts` (`sha256Hex`, `ChecksumUnsupportedError`) and `download.ts` (`parseContentDisposition`, `saveBlobAsFile`) for the upload form's pre-flight checksum and the grid's download action. `saveBlobAsFile` takes an already-resolved file name instead of the header plus a fallback, so precedence stays with the caller; its object-URL revoke is deferred a tick because revoking synchronously can abort the download before the browser picks the blob up. |
| 2026-11-01 | agent | Module 06 Phase 0: added `formatFileSize` to `format.ts` (Latin digits, Arabic units, `—` for a missing or negative size) for the attachments grid and the versions dialog. |
| 2026-10-28 | agent | Added `useTableState().clearQuery()` after the document registry's «مسح البحث والمرشحات» button cleared filters but kept the search; both setters ran in one tick from the same URL snapshot. |
| 2026-10-28 | agent | Added `formatShortId` for compact GUID display in the users grid fallback cells. |
| 2026-10-27 | agent | Implemented the table-state foundation (`useTableState`, `useDebouncedValue`, `columnPreferences`), Damascus `date`/`format` helpers, and `jwt` claim decoding for the forced password-change hint. |
| 2026-10-27 | agent | Implemented `cn.ts` and the `theme.ts` store (light/dark/system + `prefers-color-scheme`) used by the theme toggle and `ThemeProvider`. |
| 2026-10-25 | Buffy | Added `columnPreferences.ts` for the versioned per-table `localStorage` column chooser and documented its view-state-only rule. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/lib` segment and documented pure utility files. |
