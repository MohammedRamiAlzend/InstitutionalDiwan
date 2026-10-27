# institutionaldiwan.front / src/shared/types — Shared Generic TypeScript Contracts
> **Status:** Partially implemented — `api.ts` (ProblemDetails, PagedResult, paging params); `lookup.ts` lands with the lookups slice.
## Purpose
Defines domain-agnostic TypeScript interfaces used across multiple FSD layers, such as RFC 7807 `ProblemDetails`, `LookupItem` (matching `LookupEntity` in the backend), and paged list envelopes.
## Contents
| File | Purpose |
| --- | --- |
| `api.ts` | `ProblemDetails`, `PagedResult<T>` (matches Application `PagedResult`), and the common paged/sort query param contract. |
| `lookup.ts` | `LookupItem` (`id: number`, `desc: string`, `descAr: string`, `isActive: boolean`) base interface for all 34 backend lookup tables. (❌ pending) |
| `index.ts` | Public barrel exporting shared types. |
## Implementation status
| Item | State |
| --- | --- |
| `api.ts`, `index.ts` | ✅ Implemented |
| `lookup.ts` | ❌ Pending the lookups slice |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Implemented `api.ts` and the barrel for the Identity slice. |
| 2026-10-25 | Cursor | Renamed documented paging envelope to `PagedResult<T>` to match the backend Application contract. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/types` segment. |
