# `src/widgets/department-records-panel — Department Records Panel`

> **Status:** Implemented; build and lint pass.

## Purpose

The «سجل الديوان» tab of a document: what each department recorded on each of its visits, as a
timeline per department (newest visit first), with the open or locked state, the form version, the
last correction and the record values.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DepartmentRecordsPanel.tsx` | One card per department register line; each visit shows its date, lock badge, values (fields hidden by a condition are left out) and, when the server allows, «تعديل السجل» or «تصحيح» opening the form inline. |
| `index.ts` | Public barrel. |

## Notes

- The server returns only the records the caller may read (own department, or
  `DEPARTMENT_RECORD_VIEW_ALL`); the details page hides the tab when there is none.
- A visit opened when the department had no record form says so instead of listing fields.

## Implementation status

| Item | State |
| --- | --- |
| Panel | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-10 | Claude | Created the panel. |
