# institutionaldiwan.front / src/entities/ui-label — UI Label Overrides Read Model

> **Status:** Implemented (UI labels plan, Phase 1); browser verification pending manual login.

## Purpose

Owns the typed read of the system-wide UI label overrides through `GET /api/ui-labels` (any
authenticated caller). The response carries only the keys an administrator changed, plus a
`version` (the newest override's `UpdatedAt` ticks, 0 when empty). Writes (`PUT`/`DELETE`) and the
inline editor belong to `features/ui-label-manage` (Phase 2).

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `UiLabelOverridesResponse`: `version: number` + `overrides: Record<string, string>`. |
| `api/uiLabelApi.ts` | `uiLabelApi.get` fetches the overrides through the shared HTTP client. |
| `model/queries.ts` | `uiLabelKeys.all/overrides()` and `useUiLabelsQuery` (5-minute stale time, no retry, optional `enabled`). |
| `index.ts` | Public exports for the response type, query keys, and read hook. |

## Conventions / Notes

- Imports only `shared` and packages; no sibling entity imports or mutations.
- `app/providers/LabelsBootGate` owns applying the result to the labels store
  (`shared/lib/labels.ts`) and caching it under `diwan.uiLabels.v1`; this slice only reads.
- The write feature `features/ui-label-manage` (Phase 2) applies the returned snapshot to the store at
  once and invalidates `uiLabelKeys.overrides()` after a successful save/reset so the gate re-applies
  the fresh snapshot on its next background read.

## Implementation status

| Item | State |
| --- | --- |
| Overrides read, response type, query keys, public barrel | ✅ Implemented. |
| Boot-gate integration (cache-first reveal + background revalidation) | ✅ Implemented in `app/providers/LabelsBootGate`. |
| Browser verification | Pending manual login. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-17 | Qoder | UI labels Phase 2: noted that `features/ui-label-manage` now owns the writes (apply snapshot + invalidate `uiLabelKeys.overrides()`); this read slice is unchanged. |
| 2026-11-17 | Qoder | Created the slice for UI labels Phase 1 (`plans/ui-labels-edit-mode.md`): `UiLabelOverridesResponse`, `uiLabelApi.get`, `uiLabelKeys`, `useUiLabelsQuery`. |