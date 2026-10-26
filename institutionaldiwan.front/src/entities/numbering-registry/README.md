# institutionaldiwan.front / src/entities/numbering-registry — Official Numbering Registry Entity Slice
> **Status:** Scaffolding & Rules — Numbering registry read models and queries defined; code pending.
## Purpose
Models Module 04 `NumberingRegistry` and `NumberingCounter` (`NumberingRegistryResponse`). Owns `GET /api/numbering/registries` and `GET /api/numbering/registries/{registryId}` read queries and registry scope/counter cards.
## Contents
| File | Purpose |
| --- | --- |
| `model/types.ts` | `NumberingRegistryResponse` and `NumberingCounterResponse` TypeScript contracts. |
| `model/queries.ts` | `numberingRegistryKeys` factory and `useNumberingRegistries`, `useNumberingRegistryById` hooks. |
| `api/numberingRegistryApi.ts` | Typed `GET` calls to `/api/numbering/registries`. |
| `ui/RegistryScopeCard.tsx` | Displays registry code, format pattern, reset policy, and per-year sequence counters. |
| `index.ts` | Public barrel exporting types, query keys, hooks, and UI components. |
## Implementation status
| Item | State |
| --- | --- |
| `model/`, `api/`, `ui/`, `index.ts` | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/entities/numbering-registry` slice folder and README. |
