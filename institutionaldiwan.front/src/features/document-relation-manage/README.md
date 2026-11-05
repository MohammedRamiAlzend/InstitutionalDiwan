# `src/features/document-relation-manage` — Relation Writes

> **Status:** Implemented — add and remove a document link (Module 08).

## Purpose

Write side of Module 08: link the current document to another one and remove a link with a recorded reason.

## Contents

| File | Purpose |
| --- | --- |
| `api/relationMutations.ts` | `useCreateRelationMutation` (`POST .../relations`) and `useRemoveRelationMutation` (`DELETE .../relations/{id}` with the reason in the JSON body); both invalidate `relationKeys.all`. |
| `model/schemas.ts` | Zod schemas: target + type required, notes ≤ 500; removal reason required, ≤ 500. |
| `model/types.ts` | `CreateDocumentRelationRequest`, `RemoveDocumentRelationRequest`. |
| `ui/DocumentPicker.tsx` | Debounced registry search that picks the target document, excluding the current and cancelled ones. |
| `ui/CreateRelationDialog.tsx` | Target picker, relation type select and optional notes; server errors shown in Arabic. |
| `ui/RemoveRelationDialog.tsx` | Mandatory-reason confirmation that removes one link. |
| `index.ts` | Public barrel. |

## Notes

- A link is visible from both documents, so every cached relation read is invalidated, not only the route document's.
- «يلغي» is informational: it changes no document status.

## Implementation status

| Item | State |
| --- | --- |
| Create / remove dialogs, picker, schemas, mutations | ✅ Implemented (build and lint pass; accepted as verified by the project owner, 2026-11-03) |

## Change Log

- 2026-11-05: Unsent form input is now kept (per user, per form) after an accidental close or cancel and restored with a toast; it is cleared only after a successful submit (`useDraftForm`).
| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | agent | Implemented `document-relation-manage` for Module 08. |
