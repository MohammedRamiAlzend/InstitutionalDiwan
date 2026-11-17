# institutionaldiwan.front / src/features/ui-label-manage — Inline UI Label Editing

> **Status:** Implemented (UI labels plan, Phase 2); browser verification pending manual login.

## Purpose

Owns every write to the system-wide UI label dictionary and the in-place editor that performs
them: `PUT /api/ui-labels/{key}` (upsert one override) and `DELETE /api/ui-labels/{key}` (idempotent
reset to the built-in default). Both endpoints return the full override set, which is applied to the
labels store at once so every `<Label>` and key-driven surface repaints immediately. The server
guards both writes to the **superadmin account** (`SUPER_ADMIN` role + username `superadmin`); anyone
else gets `403`. Reads stay in `entities/ui-label`.

## Contents

| File | Purpose |
| --- | --- |
| `model/types.ts` | `SetUiLabelOverrideRequest` (`{ value }`) and `SetUiLabelOverrideVariables` (`{ key, value }`). |
| `model/schemas.ts` | `UI_LABEL_MAX_LENGTH` (200) and `uiLabelValueSchema(defaultText)` — a per-key Zod schema enforcing non-empty, ≤200, no HTML tags, and parity with the default's `{name}` placeholders (the client-side guards the server does not check). |
| `api/uiLabelMutations.ts` | `useSetUiLabelMutation` / `useResetUiLabelMutation`: optimistic apply → confirm from the response → rollback on error, then invalidate `uiLabelKeys.overrides()`. |
| `ui/LabelEditor.tsx` | The in-place editor: input + save/cancel/reset, Enter saves, Esc cancels, trapped focus, Arabic validation/server errors, success toasts; clicks are kept from navigating the surrounding link. |
| `ui/UiLabelEditProvider.tsx` | Provides the editor into the shared `LabelEditContext` bridge so any `<Label>` can open it. Mounted by `app/providers/AppProviders`. |
| `index.ts` | Public barrel (types, schema, mutations, editor, provider). |

## Conventions / Notes

- Imports only `@/entities/ui-label`, `@/shared/*` barrels, and packages — no upward or sibling imports.
- The shared `<Label>` primitive cannot import this feature (FSD), so the editor reaches it through
  `LabelEditContext` (`shared/ui/label-edit-context.ts`): the app provides the renderer, `<Label>` consumes it.
- Edit mode itself is a per-browser preference (`shared/lib/labelEdit.ts`, `diwan.labelEdit`); the boot
  gate forces it off for non-superadmin sessions and the shell toggle is absent from the DOM for them.
- After a write the store is confirmed from the authoritative response and the query is invalidated so
  `LabelsBootGate` re-applies the same fresh snapshot on its next background read.

## Implementation status

| Item | State |
| --- | --- |
| Upsert/reset mutations with optimistic apply + rollback + invalidation | ✅ Implemented |
| Per-key validation (length, placeholders, no HTML) | ✅ Implemented |
| In-place editor (save/cancel/reset, keyboard, focus trap) | ✅ Implemented |
| Context bridge provider | ✅ Implemented |
| Browser verification (superadmin edit/reset, placeholder rejection, non-superadmin 403) | Pending manual login |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-17 | Qoder | Created the slice for UI labels Phase 2 (`plans/ui-labels-edit-mode.md`): upsert/reset mutations, `uiLabelValueSchema`, `LabelEditor`, and `UiLabelEditProvider` over the shared `LabelEditContext` bridge. |