# `src/features/department-record-manage — Department Record Write`

> **Status:** Implemented; build and lint pass.

## Purpose

Edits the record a department keeps on one visit of a document, and corrects a locked record with a
reason. The inputs, validation and read model live in `entities/document-schema`, shared with the
creation form and the receive dialog.

## Contents

| File | Purpose |
| --- | --- |
| `api/recordMutations.ts` | `PUT /api/registry-visits/{visitId}/fields` (edit an open record) and `PUT …/fields/correct` (correct a locked record with a reason); both refresh that document's department records. |
| `ui/EditVisitRecordForm.tsx` | Form over the visit's pinned record form with local validation; in correction mode it also asks for a reason (required, up to 1000 characters). |
| `index.ts` | Public barrel. |

## Notes

- Every field is sent, so a blanked value clears it; required fields must stay filled.
- Editing needs `DOCUMENT_EDIT` and a scope covering the record's department; the server answers
  `DepartmentRecords.VisitLocked` once the document has left. Correction needs
  `DEPARTMENT_RECORD_CORRECT`. The panel shows each action only when the server's `canEdit` /
  `canCorrect` allow it.

## Implementation status

| Item | State |
| --- | --- |
| Edit open record | ✅ Implemented |
| Correct locked record with reason | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-10 | Claude | Created the slice: edit and correction mutations and the record form. |
