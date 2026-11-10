# Application / Features / DepartmentRecords

> **Status:** Implemented — plan [`plans/department-records.md`](../../../plans/department-records.md).
> Backend, persistence, HTTP surface and tests are in place.

## Purpose

When a department receives a document that another department created, it keeps its **own record**
of what happened while it held the document, on the same document (the document is never copied).
The record lives on the register visit (`DocumentRegistryVisit`) the receipt wrote, so a document
that comes back to the department gets a new visit with an independent record.

The record's fields come from the department's own **record form** for the document type: a schema
version of the dynamic-field engine with `DepartmentId` set, administered with `LOOKUP_MANAGE` like
the creation form. A department without a published form for the type receives exactly as before.

Life cycle of one record:

1. **Opened at receipt.** `ReceiveDocumentCommand.FieldValues` is validated against the published
   form for (type, receiving department); every required field must be filled or the receipt fails.
   The visit is pinned to that form version (`SchemaVersionId`).
2. **Edited while the document stays.** Only the department that keeps the record (its scope) with
   `DOCUMENT_EDIT` can change it; required fields stay required.
3. **Locked when the document leaves.** Send, redirect, reject and return lock the record of the
   department the document leaves (`LockedAt`). A receipt also locks any other record still open on
   the document (custody is exclusive), and archiving or cancelling locks them all.
4. **Corrected only with a reason.** `DEPARTMENT_RECORD_CORRECT` (administrators) may correct a
   locked record; the reason is stored on the visit and every write is audited.

A movement returned to its sender gives the sender a new visit dated the return day on its line for
that year (no receipt follows a return), with its form opened and required fields enforced on save.

## Contents

| File | Purpose |
| --- | --- |
| `DepartmentRecordService.cs` | The single place that opens a record (receipt, return to sender) and locks records (leaving a department, receipt elsewhere, archive, cancel); every movement and document handler calls it inside its own unit of work. |
| `DepartmentRecordCommands.cs` | `SetVisitRecordCommand`, `CorrectVisitRecordCommand`, `GetDocumentDepartmentRecordsQuery` and their FluentValidation validators. |
| `DepartmentRecordHandlers.cs` | Edit and correction handlers (with `VisitLoader`, which finds a visit's document and line) and the query handler, which returns a department's records only to callers whose scope covers it or who hold `DEPARTMENT_RECORD_VIEW_ALL` and may view the document. |
| `DepartmentRecordResponses.cs` | `DocumentDepartmentRecordsResponse`, `DepartmentRecordResponse` (one register line) and `DepartmentRecordVisitResponse` (one visit with its form version, lock state, correction stamp, `CanEdit`/`CanCorrect` and field values as text). |

## Notes

- Visit values live in `DEPARTMENT_RECORD_VALUES`, owned by the visit, never in `DOCUMENT_FIELD_VALUES`,
  so the document's creation values, their registry filters and columns are untouched.
- Schema validation, visibility conditions and text formatting reuse the creation-form code: a stored
  record value is turned into a transient `DocumentFieldValue` (`DepartmentRecordValue.AsFieldValue`).
- Two hand-written visits cannot share a date on one register line; arrivals tied to different
  movement steps can (out and back on the same day).

## Implementation status

| Item | State |
| --- | --- |
| Open at receipt with required fields | ✅ |
| Lock on send, redirect, reject, return, receipt elsewhere, archive, cancel | ✅ |
| Return to sender opens a new sender visit | ✅ |
| Edit (own department) and correction (with reason) | ✅ |
| Visibility (own department, `DEPARTMENT_RECORD_VIEW_ALL`) | ✅ |
| Print sheet and registry export of record fields | ⏳ Deferred (plan open point 5) |
| Registry filters on record fields | ⏳ Deferred (plan open point 6) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-10 | Claude | Created the slice: record service, edit and correction commands, the visibility-filtered query and responses. |
