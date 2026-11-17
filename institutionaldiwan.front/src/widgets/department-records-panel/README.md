# `src/widgets/department-records-panel — Department Records Panel`

> **Status:** Implemented; build and lint pass.

## Purpose

The «سجل الديوان» block inside the document details «السجلات» section: what each department
recorded on each of its visits, as a timeline per department (newest visit first), with the open
or locked state, the form version, the last correction and the record values.

## Contents

| File | Purpose |
| --- | --- |
| `ui/DepartmentRecordsPanel.tsx` | One borderless block per department register line, separated by light dividers; each visit shows its date, lock badge, values (fields hidden by a condition are left out) and, when the server allows, «تعديل السجل» or «تصحيح» opening the form inline. |
| `index.ts` | Public barrel. |

## Notes

- The server returns only the records the caller may read (own department, or
  `DEPARTMENT_RECORD_VIEW_ALL`); the details page hides the block when there is none.
- A visit opened when the department had no record form says so instead of listing fields.

## Implementation status

| Item | State |
| --- | --- |
| Panel | ✅ Implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-17 | Qoder | De-nested the per-department cards: the bordered `bg-surface` card became a borderless block with a light top divider between departments, so the panel no longer nests a card inside the «السجلات» section card. |
| 2026-11-10 | Claude | Created the panel. |
