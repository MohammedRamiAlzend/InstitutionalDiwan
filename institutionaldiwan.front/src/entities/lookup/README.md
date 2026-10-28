# institutionaldiwan.front / src/entities/lookup — Reference Lookups Catalog & Deterministic IDs Slice
> **Status:** ✅ Implemented (2026-10-27) — generic catalog reads, seeded fallbacks, deterministic IDs, and `LookupLabel`.
## Purpose
Reads the seeded reference ("lookup") catalogs through the generic `GET /api/lookups/{lookup}` route and resolves `(lookup, id)` pairs to Arabic labels. Module 02 consumes four catalogs directly (`user-scope-types`, `assignment-types`, `department-types`, `access-levels`); the same generic client serves every other slug the API exposes.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `LookupEntryResponse` (API contract) and `LookupCatalogEntry` (`id`/`desc`/`descAr` fallback row). |
| `model/constants.ts` | Re-exports `USER_SCOPE_TYPE`, `ASSIGNMENT_TYPE`, `DEPARTMENT_TYPE` from `shared/config`; `LOOKUP_SLUGS`, and `LOOKUP_FALLBACKS` mirroring `InstitutionalDiwan.Infrastructure/Persistence/Seed` for the four Module 02 catalogs. |
| `api/lookupApi.ts` | `entries(lookup, includeInactive?)` → `GET /api/lookups/{lookup}`. |
| `model/queries.ts` | `lookupKeys` factory and `useLookupEntries(lookup, { includeInactive, enabled })` with `staleTime: Infinity` and seeded placeholder data (active-only reads). |
| `ui/LookupLabel.tsx` | Renders the Arabic label for an `(lookup, id)` pair from the live catalog, falling back to the seeded row, then `#id`; `null` renders `—`. |
| `index.ts` | Public barrel exporting types, IDs/slugs/fallbacks, query keys/hook, and `LookupLabel`. |
## Notes
- Deterministic IDs live in `src/shared/config/lookupIds.ts` so sibling entities (for example `entities/user`) can map `scopeTypeId` without importing another entity slice.
- Fallbacks keep labels usable before the first response and for callers without `LOOKUP_MANAGE`; the server remains the source of truth.
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `api/`, `ui/`, `index.ts` | ✅ Implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-27 | agent | Implemented Phase 2 lookup reads for Module 02: generic catalog client, query hook with seeded placeholders, `LookupLabel`, and deterministic IDs moved to `shared/config`. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/lookup` slice folder and README. |
