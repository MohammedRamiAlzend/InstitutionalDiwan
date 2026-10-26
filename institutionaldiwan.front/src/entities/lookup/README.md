# institutionaldiwan.front / src/entities/lookup — Reference Lookups Catalog & Deterministic IDs Slice
> **Status:** Scaffolding & Rules — Lookup catalog constants and label resolver defined; code pending.
## Purpose
Provides deterministic integer IDs, Arabic/English fallback catalogs (matching `InstitutionalDiwan.Infrastructure/Persistence/Seed/*LookupSeeders.cs`), and TanStack Query hooks (`GET /api/lookups/{lookup}`) for all 34 reference tables in `InstitutionalDiwan.Domain`.
## Contents
| File | Purpose |
| --- | --- |
| `model/constants.ts` | Deterministic integer IDs and seeded `(id, desc, descAr)` catalogs for `DocumentDirection`, `DocumentType`, `DocumentStatusType`, `DocumentConfidentialityLevel`, `DocumentPriorityLevel`, `DepartmentType`, `UserScopeType`, `AccessLevel`, `NumberingResetPolicy`, etc. |
| `model/queries.ts` | `lookupKeys` and `useLookupEntries(lookupName)` with `staleTime: Infinity` and seeded fallback data. |
| `ui/LookupLabel.tsx` | Renders the localized Arabic (`descAr`) or English (`desc`) label for any `(lookup, id)` pair. |
| `index.ts` | Public barrel exporting lookup constants, hooks, and `LookupLabel`. |
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `ui/`, `index.ts` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/lookup` slice folder and README. |
