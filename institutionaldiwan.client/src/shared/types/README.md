# institutionaldiwan.client / src/shared/types — Shared Generic TypeScript Contracts
> **Status:** Scaffolding & Rules — Shared type contracts defined; code pending.
## Purpose
Defines domain-agnostic TypeScript interfaces used across multiple FSD layers, such as RFC 7807 `ProblemDetails`, `LookupItem` (matching `LookupEntity` in the backend), and paged list envelopes.
## Contents
| File | Purpose |
| --- | --- |
| `api.ts` | `ProblemDetails`, `PagedResponse<T>`, and sorting/pagination query param contracts. |
| `lookup.ts` | `LookupItem` (`id: number`, `desc: string`, `descAr: string`, `isActive: boolean`) base interface for all 34 backend lookup tables. |
| `index.ts` | Public barrel exporting shared types. |
## Implementation status
| Item | State |
| --- | --- |
| `api.ts`, `lookup.ts` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/types` segment. |
