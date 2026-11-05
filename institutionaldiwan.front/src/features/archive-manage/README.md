# `src/features/archive-manage` — Archive Commands and Saved Searches

> **Status:** Implemented — archive/unarchive and saved-search writes.

## Purpose

All Module 07 writes: archiving a registered document, restoring it, and managing the caller's
private saved searches. Each mutation invalidates the entity query keys it affects.

## Contents

| File | Purpose |
| --- | --- |
| `api/archiveMutations.ts` | Mutation hooks for `POST /api/documents/{id}/archive` and `/unarchive`, and `POST`/`PUT`/`DELETE /api/saved-searches`; archive transitions invalidate document details, registry lists, archive searches and the document's history. |
| `model/types.ts` | Request bodies: `ArchiveDocumentRequest`, `UnarchiveDocumentRequest`, `SaveSearchRequest`. |
| `model/schemas.ts` | Zod schemas with Arabic messages (location required, reasons ≤ 500, unarchive reason required, search name ≤ 150). |
| `ui/ArchiveDocumentDialog.tsx` | Location select (from `archive-locations`) plus optional reason. |
| `ui/UnarchiveDocumentDialog.tsx` | Mandatory-reason dialog that restores the pre-archive status. |
| `ui/SaveSearchDialog.tsx` | Names the grid's current criteria, optionally as the default; also renames/re-captures an existing search. |
| `ui/DeleteSavedSearchDialog.tsx` | Confirmation gate before permanently deleting a saved search. |
| `index.ts` | Public barrel. |

## Permissions

- Archive and unarchive: `DOCUMENT_ARCHIVE` plus `MANAGE` access on the document (enforced by the server; the page hides the buttons without `DOCUMENT_ARCHIVE`).
- Saved searches: `DOCUMENT_VIEW`, private to the caller.

## Implementation status

| Item | State |
| --- | --- |
| Mutations, schemas, dialogs | ✅ Implemented (build and lint pass; verified against the live API) |

## Change Log

- 2026-11-05: Unsent form input is now kept (per user, per form) after an accidental close or cancel and restored with a toast; it is cleared only after a successful submit (`useDraftForm`).
| Date | Author | Change |
| --- | --- | --- |
| 2026-11-03 | agent | Implemented the Module 07 commands and dialogs. |
