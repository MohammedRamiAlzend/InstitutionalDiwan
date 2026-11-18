# `src/features/document-field-values — Document Field Values Write`

> **Status:** Implemented and verified in-browser (plan Phase 4).

## Purpose

Saves the dynamic field values of a draft document. The shared inputs and validation live in `entities/document-schema` so `features/document-create` can reuse them without importing a sibling feature.

## Contents

| File | Purpose |
| --- | --- |
| `api/fieldValueMutations.ts` | `PUT /api/documents/{id}/fields`; writes the answer straight into the query cache. |
| `ui/EditFieldValuesForm.tsx` | Form over the pinned schema with local validation before the request and an Arabic error banner. |
| `index.ts` | Public barrel. |

## Notes

- Every field is sent so blanked values are cleared; blank becomes `null`.
- Needs `DOCUMENT_EDIT` and DOWNLOAD access; registered documents are locked server-side.

## Implementation status

| Item | State |
| --- | --- |
| Edit values form | ✅ Implemented; amount edit verified in-browser |
| UI labels migration (ب4) | ✅ Save/back buttons and the success toast resolve from the `schema.*` catalog (2026-11-18) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (ب4): `EditFieldValuesForm` save/back buttons via `<Label>` (`schema.panel.save-values` / `schema.action.back`) and the success toast via `t()`. |
| 2026-11-08 | Claude | Read-only fields show their fixed value and are not sent. |
| 2026-11-03 | Claude | Implemented per plan `plans/document-dynamic-fields-frontend.md`. |
