# institutionaldiwan.front / src/entities/numbering-registry — Official Numbering Registry Entity Slice

> **Status:** Implemented (Module 03 Phase 5, plus Module 04 Phase 1 model helpers) — registry/counter
> read models, query keys/hooks, the scope card, the read-only department register book, and pure
> template/scope helpers.

## Purpose

Models Module 04 `NumberingRegistry` and `NumberingCounter` (`NumberingRegistryResponse`). Owns
`GET /api/numbering/registries`, `GET /api/numbering/registries/{registryId}`, and the read-only
`GET /api/departments/{departmentId}/register` queries, plus presentational registry-scope UI.
Writes live in `features/numbering-manage`.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `NumberingRegistryResponse`, `NumberingCounterResponse`, `DepartmentRegisterLineResponse`, and `DepartmentRegisterParams` TypeScript contracts. |
| `model/queries.ts` | `numberingRegistryKeys` factory and `useNumberingRegistries`, `useNumberingRegistryById`, `useDepartmentRegister` hooks. |
| `model/format.ts` | Pure `renderNumberPattern(pattern, { numberingYear, sequenceValue })` mirroring `OfficialNumberFormatter` (`{YEAR}`, `{SEQ}`, `{SEQ:Dn}`, case-insensitive, `DEFAULT_NUMBER_PATTERN` on blank), the default template constant, and `unknownPatternTokens` (tokens the formatter cannot expand) for form validation. |
| `model/scope.ts` | Pure scope helpers: `registryScopeRank` (constrained dimensions 0–3; ordering only, never asserted as server precedence), `registryScopeKey`, `describeRegistryScope` (Arabic summary), and `findDuplicateActiveScopes` (active registries sharing one scope triple). |
| `api/numberingRegistryApi.ts` | Typed `GET` calls to `/api/numbering/registries` and `/api/departments/{id}/register` (blank department skips the call). |
| `ui/RegistryScopeCard.tsx` | Displays registry code, format pattern, reset policy, and per-year sequence counters. |
| `index.ts` | Public barrel exporting types, query keys, hooks, and UI components. |

## Query keys

`numberingRegistryKeys.all` → `lists()` → `list()`; `details()` → `detail(id)`; `departmentRegister()`
→ `departmentRegister(departmentId, year)`. Registry writes invalidate `lists()`, `details()`, and the
affected `detail(id)`.

## Permissions

- All reads require `NUMBERING_VIEW` on the server; `/numbering` gates the page with
  `RequirePermission`.
- The slice does not gate calls itself — feature dialogs and pages hide write affordances without
  `NUMBERING_MANAGE`, and the server rejects direct writes with 403.

## Implementation status

| Item | State |
| --- | --- |
| `model/`, `api/`, `ui/`, `index.ts` | ✅ Implemented (Module 03 Phase 5) |
| `model/format.ts`, `model/scope.ts` | ✅ Implemented (Module 04 Phase 1) — pure helpers. |
| Consumers | ✅ Module 04 Phase 2 — grid filters/specificity column/duplicate banner and the form's live preview + token validation use these helpers. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Module 04 Phase 2: added `unknownPatternTokens` to `model/format.ts` and exported it through the barrel; documented that `registryScopeRank` is ordering-only, not a server-precedence claim. |
| 2026-10-29 | agent | Module 04 Phase 1: added the pure `model/format.ts` (`renderNumberPattern`, `DEFAULT_NUMBER_PATTERN`) and `model/scope.ts` (`registryScopeRank`, `registryScopeKey`, `describeRegistryScope`, `findDuplicateActiveScopes`) helpers through the public barrel; no UI change yet. |
| 2026-10-29 | agent | Module 03 Phase 5: implemented the read slice — registry/counter and department-register types, query keys/hooks, typed GETs, and `RegistryScopeCard`; verified against the live API from `/numbering`. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/numbering-registry` slice folder and README. |
