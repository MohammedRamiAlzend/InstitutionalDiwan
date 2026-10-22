# institutionaldiwan.client / src/shared/lib — Pure Utilities, Date/Timezone & Crypto Helpers
> **Status:** Scaffolding & Rules — Utility rules defined; code pending.
## Purpose
Contains pure, framework-independent helper functions for class merging, `Asia/Damascus` timezone formatting, Arabic text normalization, and client-side SHA-256 file checksum calculation (matching Module 06 `DocumentFileVersion` requirements).
## Contents
| File | Purpose |
| --- | --- |
| `cn.ts` | Combines `clsx` and `tailwind-merge` for conditional Tailwind class composition. |
| `date.ts` | Formats UTC ISO timestamps from the backend (`datetime2(3)`) into `Asia/Damascus` local dates/times and extracts the official numbering year. |
| `crypto.ts` | Computes lowercase 64-character hex SHA-256 checksums via `crypto.subtle.digest('SHA-256', buffer)` for attachment uploads. |
| `arabic.ts` | Normalizes Arabic search strings (unified hamzas, taa marbuta, and Eastern/Western numerals). |
| `index.ts` | Public barrel exporting all utility helpers. |
## Implementation status
| Item | State |
| --- | --- |
| `cn.ts`, `date.ts`, `crypto.ts`, `arabic.ts` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/lib` segment and documented pure utility files. |
