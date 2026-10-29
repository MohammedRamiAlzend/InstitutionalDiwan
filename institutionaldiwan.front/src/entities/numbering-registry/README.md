# institutionaldiwan.front / src/entities/numbering-registry — Official Numbering Registry Entity Slice

> **Status:** Implemented (Module 03 Phase 5) — registry/counter read models, query keys/hooks, the
> scope card, and the read-only department register book.

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

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-29 | agent | Module 03 Phase 5: implemented the read slice — registry/counter and department-register types, query keys/hooks, typed GETs, and `RegistryScopeCard`; verified against the live API from `/numbering`. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/numbering-registry` slice folder and README. |
