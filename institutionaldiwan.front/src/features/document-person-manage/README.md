# `src/features/document-person-manage`

> **Status:** Implemented and browser-verified. Plan: `plans/document-persons.md`.

## Purpose

Changes who is named on a document after it exists.

## Contents

| File | Purpose |
| --- | --- |
| `api/documentPersonMutations.ts` | Add, change-role and remove mutations; each invalidates `personKeys.all`. |
| `model/schemas.ts` | Zod schemas of the two dialogs. |
| `ui/AddDocumentPersonsDialog.tsx` | Adds several people in one role; keeps whatever failed in the form. |
| `ui/ChangeDocumentPersonRoleDialog.tsx` | Changes one person's role. |
| `index.ts` | Public barrel. |

## Implementation status

| Area | Status |
| --- | --- |
| Slice | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-09 | Claude | Created the slice. |
